// WeaponData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Ship/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Core Stats")]
    public string weaponName = "Default Weapon";
    public GameObject projectilePrefab;
    public Sprite icon;
    public float fireRate = 2f; // Shots per second
    public float projectileSpeed = 15f;
    public int projectileDamage = 1; // Passed to projectile if needed, or projectile has its own
    public float energyCostPerShot = 0f;

    [Header("Visuals & Audio")]
    public GameObject muzzleFlashPrefab;
    public AudioClip fireSound;
    [Range(0f, 1f)] public float fireSoundVolume = 0.7f;

    [Header("Firing Behavior")]
    public FiringMode firingMode = FiringMode.SingleTarget;

    // --- Parameters for Specific Firing Modes ---
    [Header("Spread Parameters (Used if FiringMode is Spread)")]
    [Range(1, 20)] public int projectilesInSpread = 3;
    [Range(0f, 360f)] public float spreadAngle = 30f;

    [Header("Volley Parameters (Used if FiringMode is ForwardVolley)")]
    [Range(1, 10)] public int volleyCount = 3;
    [Range(0.01f, 1f)] public float volleyDelay = 0.1f; // Delay between shots in a volley

    [Header("Spiral Parameters (Used if FiringMode is Spiral)")]
    [Range(1f, 90f)] public float spiralAngleStep = 15f; // Angle increment per shot for spiral
    public bool spiralOrientsToTarget = false; // If true, the 'forward' of the spiral aims at target
}

public enum FiringMode
{
    SingleTarget,     
    Spread,            
    ForwardVolley,     
    Spiral,            
   
}