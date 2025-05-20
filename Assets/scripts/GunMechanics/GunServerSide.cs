using System.Collections;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class GunServerSide : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GunData gunData;
    [SerializeField] private Transform muzzle;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private LayerMask hitMask;
    //[SerializeField] private BulletHoleManager bulletHoleManager;

    [Header("Effects")]
    [SerializeField] private AudioClip shootSound;
    private AudioSource _audioSource;
    [SerializeField] private GunSway gunSway;

    private Camera _playerCamera;
    private float _timeSinceLastShot;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoText;

    private void Start()
    {
        if (!IsOwner) return;

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
        _timeSinceLastShot += Time.deltaTime;
        UpdateAmmoUI();
    }

    public override void OnDestroy()
    {
        if (IsOwner)
        {
            PlayerShoot.shootInput -= Shoot;
            PlayerShoot.reloadInput -= StartReloading;
        }
    }

    public void StartReloading()
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

        Vector3 camOrigin = _playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector3 camDirection = _playerCamera.transform.forward;

        ShootServerRpc(camOrigin, camDirection);

        gunData.currentAmmo--;
        _timeSinceLastShot = 0f;
        OnGunShot();
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
        if (ammoText != null)
            ammoText.text = $"{gunData.currentAmmo} / {gunData.magazineSize}";
    }

    #region Netcode

    [ServerRpc]
    private void ShootServerRpc(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, gunData.maxDistance, hitMask))
        {
            var damageable = hit.transform.GetComponentInParent<IDamageable>();
            damageable?.Damage(gunData.damage);

            var netObj = hit.collider.GetComponentInParent<NetworkObject>();
            ulong parentId = netObj != null ? netObj.NetworkObjectId : 0;
            
            if(BulletHoleManager.Instance != null)
                BulletHoleManager.Instance.SpawnBulletHoleClientRpc(hit.point, hit.normal, direction, parentId);
        }
    }

    #endregion
}
