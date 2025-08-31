using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine.Serialization;

public class Gun : NetworkBehaviour
{
    // Flag zum Umschalten
    public static bool lagCompensationEnabled = true;

    [Header("References")]
    [SerializeField] private GunData gunData;
    [SerializeField] private Transform muzzle;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private LayerMask hitMask;

    [Header("Effects")]
    [SerializeField] private AudioClip shootSound;
    private AudioSource _audioSource;
    [SerializeField] private GunSway gunSway;

    private Camera _playerCamera;
    private float _timeSinceLastShot;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoText;

    
    [Header("Bullet Hole FX")]
    [SerializeField] private BulletHoleManager bulletManager;

    [Header("Hitmarker")] 
    private RawImage _hitmarkerImage; 
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip hitmarkerSound;
    private float _hitmarkerCooldown;
    
    // Netcode Logs
    private double _shotStartTime;
    private static string _logFilePath;
    
    #region Unity Methods
    private void Awake()
    {
        if (string.IsNullOrEmpty(_logFilePath))
        {
            string folder = Path.Combine(Application.persistentDataPath, "Logs");
            Directory.CreateDirectory(folder);
            _logFilePath = Path.Combine(folder, "gun_latency_log.csv");

            if (!File.Exists(_logFilePath))
            {
                File.WriteAllText(_logFilePath, "tick;clientPing;localShotTime;confirmTime;latencyMs;lagComp\n");
            }
        }
    }
    private void Start()
    {
        if (!IsOwner) return;

        bulletManager = FindFirstObjectByType<BulletHoleManager>();
        _hitmarkerImage = GameObject.Find("Player UI/Hitmarker/Canvas/RawImage").GetComponent<RawImage>();
        _hitmarkerImage.color = new Color(1f, 1f, 1f, 0f);

        UpdateAmmoUI();

        gunData.isReloading = false;
        gunData.currentAmmo = gunData.magazineSize;

        PlayerShoot.shootInput += Shoot;
        PlayerShoot.reloadInput += StartReloading;

        _playerCamera = Camera.main;

        _audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (InputState.InputLocked) return;

        _timeSinceLastShot += Time.deltaTime;
        UpdateAmmoUI();
        UpdateHitmarker();
    }

    private void OnDestroy()
    {
        if (IsOwner)
        {
            PlayerShoot.shootInput -= Shoot;
            PlayerShoot.reloadInput -= StartReloading;
        }
    }
    #endregion

    private void StartReloading()
    {
        if (!gunData.isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    private IEnumerator Reload()
    {
        gunData.isReloading = true;
        yield return new WaitForSeconds(gunData.reloadTime);
        gunData.currentAmmo = gunData.magazineSize;
        gunData.isReloading = false;
    }

    public bool CanShoot() =>
        !gunData.isReloading && _timeSinceLastShot > 1f / (gunData.fireRate / 60f);

    private void Shoot()
    {
        if (gunData.currentAmmo <= 0 || !CanShoot() || _playerCamera == null) return;
        
        _shotStartTime = Time.timeAsDouble;

        var camOrigin = _playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        var camDirection = _playerCamera.transform.forward;

        if (Physics.Raycast(camOrigin, camDirection, out RaycastHit camHit, gunData.maxDistance, hitMask))
        {
            ulong targetId = camHit.transform.GetComponent<NetworkObject>()?.NetworkObjectId ?? 0;

            ShootServerRpc(camOrigin, camDirection, targetId, NetworkManager.ServerTime.Time - NetworkManager.LocalTime.Time);

            bulletManager.SpawnBulletHole(camHit, new Ray(camOrigin, camDirection));
        }

        gunData.currentAmmo--;
        _timeSinceLastShot = 0f;
        OnGunShot();
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 origin, Vector3 direction, ulong targetId, double clientTimeOffset) //TODO: nochmal prüfen
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObj)) return;

        var laggedTarget = targetObj.GetComponent<Target>();
        if (laggedTarget == null) return;

        Vector3 hitPosition;

        if (lagCompensationEnabled)
        {
            double rewindTime = NetworkManager.ServerTime.Time - clientTimeOffset;
            var rewindedPos = laggedTarget.GetRewindPosition(rewindTime);
            if (!rewindedPos.HasValue) return;
            hitPosition = rewindedPos.Value;
        }
        else
        {
            hitPosition = laggedTarget.transform.position;
        }

        var collider = targetObj.GetComponent<Collider>();
        if (collider == null) return;

        Vector3 closestPoint = collider.ClosestPoint(hitPosition);

        if (Vector3.Distance(closestPoint, origin) < gunData.maxDistance)
        {
            var damageable = targetObj.GetComponent<IDamageable>();
            damageable?.Damage(gunData.damage);

            if (lagCompensationEnabled)
                ConfirmLagCompHitClientRpc(targetObj.NetworkObjectId);
            else
                ConfirmHitClientRpc();
        }
    }

    [ClientRpc]
    private void ConfirmHitClientRpc()
    {
        if (!IsOwner) return;

        double confirmTime = Time.timeAsDouble;
        double latencyMs = (confirmTime - _shotStartTime) * 1000f;
        
        LogShot(NetworkManager.LocalTime.Tick, NetworkOverlay.roundTripTime, _shotStartTime, confirmTime, latencyMs, true );
        if (_hitmarkerImage != null)
            _hitmarkerImage.color = Color.white;

        if (sfx != null && hitmarkerSound != null)
            sfx.PlayOneShot(hitmarkerSound, 2f);

        _hitmarkerCooldown = 0.5f;
    }

    [ClientRpc]
    private void ConfirmLagCompHitClientRpc(ulong targetId)
    {
        ConfirmHitClientRpc();

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObj))
        {
            var renderer = targetObj.GetComponent<Renderer>();
            if (renderer != null)
                StartCoroutine(FlashHitColor(renderer, Color.magenta, 0.2f));
        }
    }

    private IEnumerator FlashHitColor(Renderer renderer, Color color, float duration)
    {
        var originalColor = renderer.material.color;
        renderer.material.color = color;
        yield return new WaitForSeconds(duration);
        renderer.material.color = originalColor;
    }

    private void OnGunShot()
    {
        PlayShotSound();
        DisplayMuzzleFlash();
        Recoil();
    }

    private void PlayShotSound()
    {
        if (_audioSource != null && shootSound != null)
        {
            _audioSource.PlayOneShot(shootSound);
        }
    }

    private void DisplayMuzzleFlash()
    {
        muzzleFlash?.Play();
    }

    private void Recoil()
    {
        gunSway?.ApplyRecoil(new Vector3(0, 0, -0.05f), new Vector3(-2f, 1f, 0f));
    }

    private void UpdateAmmoUI()
    {
        ammoText.text = $"{gunData.currentAmmo} / {gunData.magazineSize}";
    }

    private void UpdateHitmarker()
    {
        if (_hitmarkerCooldown > 0)
            _hitmarkerCooldown -= Time.deltaTime;
        else 
            _hitmarkerImage.color = Color.Lerp(_hitmarkerImage.color, new Color(1f, 1f, 1f, 0f), Time.deltaTime * 1f);
    }
    
    private void LogShot(int tick, float clientPing, double shotTime, double confirmTime, double latencyMs, bool lagComp)
    {
        string line = $"{tick},{clientPing * 500:F0},{shotTime.ToString(CultureInfo.InvariantCulture)},{confirmTime.ToString(CultureInfo.InvariantCulture)},{latencyMs.ToString("F2", CultureInfo.InvariantCulture)},{lagComp}";
        File.AppendAllText(_logFilePath, line + "\n");
    }
}

