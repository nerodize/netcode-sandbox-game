using System;
using System.Collections;
using UnityEngine;
using TMPro;
using Unity.Netcode;

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
    BulletHoleManager bulletManager;

    private void Start()
    {
        if (!IsOwner) return;
        
        UpdateAmmoUI();
        
        bulletManager = FindFirstObjectByType<BulletHoleManager>();
       if (bulletManager == null)
            Debug.LogWarning("BulletHoleManager not found in scene.");
        
        Debug.Log("📦 Gun.cs Start aufgerufen");
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
        
        _timeSinceLastShot += Time.deltaTime;
        UpdateAmmoUI();
    }
    private void OnDestroy()
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

        if (Physics.Raycast(camOrigin, camDirection, out RaycastHit camHit, gunData.maxDistance, hitMask))
        {
            Debug.Log($"🎯 Hit: {camHit.transform.name}");

            // Schaden zufügen
            IDamageable damageable = camHit.transform.GetComponentInParent<IDamageable>();
            damageable?.Damage(gunData.damage);
            
            bulletManager.SpawnBulletHole(camHit, new Ray(camOrigin, camDirection));

            // Bullet Hole erzeugen
            //SpawnBulletHole(camHit, camDirection);

            // Optional: Hit-Effekt
            // SpawnHitEffect(camHit.point, Quaternion.LookRotation(camHit.normal));
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
