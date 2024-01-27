using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using Unity.Netcode;
using UnityEngine;

public class WeaponSwitch : NetworkBehaviour
{
    List<WeaponController> m_Weapons = new List<WeaponController>();

    [ClientRpc]
    private void SwitchWeaponClientRPC(int pIndex, bool pIsShowing, ClientRpcParams pParams = default)
    {
        m_Weapons[pIndex].ShowWeapon(pIsShowing);
    }

    public void Switch(WeaponController pWeaponController, bool pIsShowing)
    {
        SwitchWeaponClientRPC(m_Weapons.FindIndex(weapon => pWeaponController == weapon), pIsShowing);
    }

    public void AddWeaponController(WeaponController pWeaponController)
    {
        m_Weapons.Add(pWeaponController);
    }
}
