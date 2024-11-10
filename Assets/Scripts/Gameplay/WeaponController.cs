using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public Weapon[] weaponList;
    public Weapon weapon;
    private int weaponIndex = 0;
    public Transform weaponHoldTransform;

    public void EquipWeapon()
    {
        if (weapon != null)
        {
            DestroyImmediate(weapon.gameObject);
        }

        weapon = Instantiate(weaponList[Random.Range(0,2)], weaponHoldTransform.position, weaponHoldTransform.rotation, weaponHoldTransform);
    }

    public void SwitchWeapon(int index)
    {
        if (index >= 0 && index < weaponList.Length)
        {
            weaponIndex = index;
            EquipWeapon();
        }
    }
}
