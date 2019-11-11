﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Basic implementation of the pointer abstract class. Simply changes the color of the laser and 
the "cursor" on shoot.
*/

public class BasicPointer : Pointer
{
    [SerializeField]
    private Color shootColor;

    private delegate void Del();
    protected override void PlayShoot()
    {
        laser.startColor = shootColor;
        laser.endColor = shootColor;
        cursor.SetColor(shootColor);
        StartCoroutine(Wait(.1f, new Del(ResetLaser)));
    }

    private void ResetLaser()
    {
        laser.startColor = startLaserColor;
        laser.endColor = EndLaserColor;
        cursor.SetColor(EndLaserColor);
    }

    private IEnumerator Wait(float duration, Del method)
    {
        yield return new WaitForSeconds(duration);
        method();
    }
}