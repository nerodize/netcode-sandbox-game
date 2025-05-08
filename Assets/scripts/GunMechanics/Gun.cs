using System;
using System.Collections;
using UnityEditor.PackageManager;
using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunData gunData;
    [SerializeField] private Transform muzzle;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private LayerMask hitMask; // Wichtig: Exkludiere deinen eigenen Layer hier
    
    [Header("Effects")]
    [SerializeField] private AudioClip shootSound;
    private AudioSource _audioSource;
    [SerializeField] private GunSway gunSway;
    
    private Camera _playerCamera;
    private float _timeSinceLastShot;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoText;

    public GameObject hitEffectPrefab;
    
    
    private void Start()
    {
        PlayerShoot.shootInput += Shoot;
        PlayerShoot.reloadInput += StartReloading;
        
        _playerCamera = Camera.main;

        if (_playerCamera == null)
            Debug.LogError("Camera not found.");
        
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        _timeSinceLastShot += Time.deltaTime;
        UpdateAmmoUI();
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
    
    
    private bool CanShoot() =>
        !gunData.isReloading && _timeSinceLastShot > 1f / (gunData.fireRate / 60f);

    private void Shoot()
    {
        if (gunData.currentAmmo <= 0 || !CanShoot() || _playerCamera == null) return;

        // Kamera Ursprung und Richtung
        Vector3 camOrigin = _playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector3 camDirection = _playerCamera.transform.forward;

        // Raycast von der Kamera
        if (Physics.Raycast(camOrigin, camDirection, out RaycastHit camHit, gunData.maxDistance, hitMask))
        {
            
            Debug.Log($"🎯 Hit: {camHit.transform.name}");
            
            // Schaden basierend auf dem Kamera-Hit zufügen => hier InParent Zusatz verwenden weil sonst das skript nicht aufgerufen werden kann.
            // Mittlerweile redundant!!
            // TODO: fix this in unity
            IDamageable damageable = camHit.transform.GetComponentInParent<IDamageable>();
            damageable?.Damage(gunData.damage);

            // Debug Lines
            Debug.DrawLine(camOrigin, camHit.point, Color.green, 1.5f); // Kamera zu Ziel
            Debug.DrawRay(muzzle.position, (camHit.point - muzzle.position).normalized * gunData.maxDistance, Color.red, 1.5f); // Mündung zu Ziel
            
            //SpawnHitEffect(camHit.point, Quaternion.LookRotation(camHit.normal));
        }
        else
        {
            Debug.Log("❌ Nothing hit.");
        }

        gunData.currentAmmo--;
        _timeSinceLastShot = 0f;
        OnGunShot();
    }
    
    private void OnGunShot()
    {
        // Muzzle Flash, Sound etc.
        PlayShotSound();
        DisplayMuzzleFlash();
        Recoil();
    }

    private void PlayShotSound()
    {
        if (_audioSource != null && shootSound != null)
            _audioSource.PlayOneShot(shootSound);
    }

    private void DisplayMuzzleFlash()
    {
        muzzleFlash.Play();
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

    void SpawnHitEffect(Vector3 position, Quaternion rotation)
    {
        Instantiate(hitEffectPrefab, position, rotation);
    }
}
