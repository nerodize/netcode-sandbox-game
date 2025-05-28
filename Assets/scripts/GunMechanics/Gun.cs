using System;
using System.Collections;
using IngameDebugConsole;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class Gun : NetworkBehaviour
{
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
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private float destroyDelay = 10f;
    private BulletHoleManager _bulletManager;

    private void Start()
    {
        if (!IsOwner) return;
        
        if (DebugLogManager.IsConsoleOpen)
            return;

        UpdateAmmoUI();
        
        _bulletManager = FindFirstObjectByType<BulletHoleManager>();
       if (_bulletManager == null)
            Debug.LogWarning("BulletHoleManager not found in scene.");
        
        Debug.Log("Gun.cs Start aufgerufen");
        gunData.isReloading = false;
        gunData.currentAmmo = gunData.magazineSize;

        PlayerShoot.shootInput += Shoot;
        PlayerShoot.reloadInput += StartReloading;

        _playerCamera = Camera.main;
        if (_playerCamera == null)
            Debug.LogError("Camera not found.");

        _audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (InputState.InputLocked) return; 
        
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

        var camOrigin = _playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        var camDirection = _playerCamera.transform.forward;
        
        //ShootServerRpc(camOrigin, camDirection);

        
        if (Physics.Raycast(camOrigin, camDirection, out RaycastHit camHit, gunData.maxDistance, hitMask))
        {
            Debug.Log($"Hit: {camHit.transform.name}");
            
            var damageable = camHit.transform.GetComponentInParent<IDamageable>();
            damageable?.Damage(gunData.damage);
            
            _bulletManager.SpawnBulletHole(camHit, new Ray(camOrigin, camDirection));
        }
        
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
            Debug.Log("Audio played");
        }
        else
        {
            Debug.Log("No Audio played");
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
        ammoText.text = ammoText != null ? $"{gunData.currentAmmo} / {gunData.magazineSize}" : "N/A";
    }
}
