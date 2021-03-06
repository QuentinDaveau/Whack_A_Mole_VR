﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*
Spawns, references and activates the moles. Is the only component to directly interact with the moles.
*/

public class WallManager : MonoBehaviour
{
    // The Mole object to be loaded
    [SerializeField]
    private Mole moleObject;

    // The count of rows to generate
    [SerializeField]
    private int rowCount;

    // The count of columns to generate
    [SerializeField]
    private int columnCount;

    // Offest of the height of the wall
    [SerializeField]
    private float heightOffset;

    // The size of the wall
    [SerializeField]
    private Vector3 wallSize;

    // Coefficient of the X curvature of the wall. 1 = PI/2, 0 = straight line
    [SerializeField]
    [Range(0.1f, 1f)]
    private float xCurveRatio = 1f;

    // Coefficient of the Y curvature of the wall. 1 = PI/2, 0 = straight line
    [SerializeField]
    [Range(0.1f, 1f)]
    private float yCurveRatio = 1f;

    // The angle of the edge moles if a curve ratio of 1 is given
    [SerializeField]
    [Range(0f, 90f)]
    private float maxAngle = 90f;

    // The scale of the Mole. Idealy shouldn't be scaled on the Z axis (to preserve the animations)
    [SerializeField]
    private Vector3 moleScale = Vector3.one;

    private class StateUpdateEvent: UnityEvent<bool, Dictionary<int, Mole>> {};
    private StateUpdateEvent stateUpdateEvent = new StateUpdateEvent();
    private WallGenerator wallGenerator;
    private Vector3 wallCenter;
    private Dictionary<int, Mole> moles;
    private bool active = false;
    private bool isInit = false;
    private float updateCooldownDuration = .1f;
    private LoggerNotifier loggerNotifier;

    void Start()
    {
        // Initialization of the LoggerNotifier.
        loggerNotifier = new LoggerNotifier(persistentEventsHeadersDefaults: new Dictionary<string, string>(){
            {"WallRowCount", "NULL"},
            {"WallColumnCount", "NULL"},
            {"WallSizeX", "NULL"},
            {"WallSizeY", "NULL"},
            {"WallSizeZ", "NULL"},
            {"WallCurveRatioX", "NULL"},
            {"WallCurveRatioY", "NULL"}
        });

        loggerNotifier.InitPersistentEventParameters(new Dictionary<string, object>(){
            {"WallRowCount", rowCount},
            {"WallColumnCount", columnCount},
            {"WallSizeX", wallSize.x},
            {"WallSizeY", wallSize.y},
            {"WallSizeZ", wallSize.z},
            {"WallCurveRatioX", xCurveRatio},
            {"WallCurveRatioY", yCurveRatio}
        });

        moles = new Dictionary<int, Mole>();
        wallGenerator = gameObject.GetComponent<WallGenerator>();
        wallCenter = new Vector3(wallSize.x/2f, wallSize.y/2f, 0);
        isInit = true;
    }

    void OnValidate()
    {
        UpdateWall();
    }

    public void Enable()
    {
        active = true;

        if (moles.Count == 0)
        {
            GenerateWall();
        }
    }

    public void Disable()
    {
        active = false;
        disableMoles();
    }

    public void Clear()
    {
        active = false;
        DestroyWall();
        stateUpdateEvent.Invoke(false, moles);
    }

    // Activates a random Mole for a given lifeTime and set if is fake or not
    public void ActivateRandomMole(float lifeTime, float moleExpiringDuration, bool isFake)
    {
        if (!active) return;

        GetRandomMole().Enable(lifeTime, moleExpiringDuration, isFake);
    }

    // Activates a specific Mole for a given lifeTime and set if is fake or not
    public void ActivateMole(int moleId, float lifeTime, float moleExpiringDuration, bool isFake)
    {
        if (!active) return;
        if (!moles.ContainsKey(moleId)) return;
        moles[moleId].Enable(lifeTime, moleExpiringDuration, isFake);
    }

    // Pauses/unpauses the moles
    public void SetPauseMole(bool pause)
    {
        foreach(Mole mole in moles.Values)
        {
            mole.SetPause(pause);
        }
    }

    public void UpdateMoleCount(int newRowCount = -1, int newColumnCount = -1)
    {
        if (newRowCount >= 2) rowCount = newRowCount;
        if (newColumnCount >= 2) columnCount = newColumnCount;
        // UpdateWall();
    }

    public void UpdateWallSize(float newWallSizeX = -1, float newWallSizeY = -1, float newWallSizeZ = -1)
    {
        if (newWallSizeX >= 0) wallSize.x = newWallSizeX;
        if (newWallSizeY >= 0) wallSize.y = newWallSizeY;
        if (newWallSizeZ >= 0) wallSize.z = newWallSizeZ;
        // UpdateWall();
    }

    public void UpdateWallCurveRatio(float newCurveRatioX = -1, float newCurveRatioY = -1)
    {
        if (newCurveRatioX >= 0 && newCurveRatioX <= 1 ) xCurveRatio = newCurveRatioX;
        if (newCurveRatioY >= 0 && newCurveRatioY <= 1 ) yCurveRatio = newCurveRatioY;
        // UpdateWall();
    }

    public void UpdateWallMaxAngle(float newMaxAngle)
    {
        if (newMaxAngle >= 0 && newMaxAngle <= 90 ) maxAngle = newMaxAngle;
        // UpdateWall();
    }

    public void UpdateMoleScale(float newMoleScaleX = -1, float newMoleScaleY = -1, float newMoleScaleZ = -1)
    {
        if (newMoleScaleX >= 0) moleScale.x = newMoleScaleX;
        if (newMoleScaleY >= 0) moleScale.y = newMoleScaleY;
        if (newMoleScaleZ >= 0) moleScale.z = newMoleScaleZ;
        // UpdateWall();
    }

    public UnityEvent<bool, Dictionary<int, Mole>> GetUpdateEvent()
    {
        return stateUpdateEvent;
    }

    // Returns a random, inactive Mole. Can block the game if no Mole can be found. May need to be put in a coroutine.
    private Mole GetRandomMole()
    {
        Mole mole;
        Mole[] tempMolesList = new Mole[moles.Count];
        moles.Values.CopyTo(tempMolesList, 0);
        do
        {
            mole = tempMolesList[Random.Range(0, moles.Count)];
        }
        while (!mole.CanBeActivated());
        return mole;
    }

    private void disableMoles()
    {
        foreach(Mole mole in moles.Values)
        {
            mole.Reset();
        }
    }

    private void DestroyWall()
    {
        foreach(Mole mole in moles.Values)
        {
            Destroy(mole.gameObject);
        }
        moles.Clear();
    }

    // Generates the wall of Moles
    private void GenerateWall()
    {
        wallGenerator.InitPointsLists(columnCount, rowCount);
        // Updates the wallCenter value
        wallCenter = new Vector3(wallSize.x/2f, wallSize.y/2f, 0);

        // For each row and column:
        for (int x = 0; x < columnCount; x++)
        {
            for (int y = 0; y < rowCount; y++)
            {
                if((x == 0 || x == columnCount - 1) && (y == rowCount - 1 || y == 0))
                {
                    wallGenerator.AddPoint(x, y, DefineMolePos(x, y), DefineMoleRotation(x, y));
                    continue;
                }

                // Instanciates a Mole object
                Mole mole = Instantiate(moleObject, transform);
                // Get the Mole object's local position depending on the current row, column and the curve coefficient
                Vector3 molePos = DefineMolePos(x, y);

                // Sets the Mole local position, rotates it so it looks away from the wall (affected by the curve)
                mole.transform.localPosition = molePos;
                mole.transform.localRotation = DefineMoleRotation(x, y);
                // Sets the Mole ID, scale and references it
                int moleId = GetMoleId(x, y);
                mole.SetId(moleId);
                mole.SetNormalizedIndex(GetnormalizedIndex(x, y));
                mole.transform.localScale = moleScale;
                moles.Add(moleId, mole);

                wallGenerator.AddPoint(x, y, molePos, mole.transform.localRotation);
            }
        }
        stateUpdateEvent.Invoke(true, moles);
        wallGenerator.GenerateWall();
    }

    // Updates the wall
    private void UpdateWall()
    {
        if (!(active && isInit)) return;
        StopAllCoroutines();
        StartCoroutine(WallUpdateCooldown());
    }

    // Gets the Mole position depending on its index, the wall size (x and y axes of the vector3), and also on the curve coefficient (for the z axis).
    private Vector3 DefineMolePos(int xIndex, int yIndex)
    {
        float angleX = ((((float)xIndex/(columnCount - 1)) * 2) - 1) * ((Mathf.PI * xCurveRatio) / 2);
        float angleY = ((((float)yIndex/(rowCount - 1)) * 2) - 1) * ((Mathf.PI * yCurveRatio) / 2);

        return new Vector3(Mathf.Sin(angleX) * (wallSize.x / (2 * xCurveRatio)), Mathf.Sin(angleY) * (wallSize.y / (2 * yCurveRatio)), ((Mathf.Cos(angleY) * (wallSize.z)) + (Mathf.Cos(angleX) * (wallSize.z))));
    }

    private int GetMoleId(int xIndex, int yIndex)
    {
        return ((xIndex + 1) * 100) + (yIndex + 1);
    }

    private Vector2 GetnormalizedIndex(int xIndex, int yIndex)
    {
        return (new Vector2((float)xIndex / (columnCount - 1), (float)yIndex / (rowCount - 1)));
    }

    // Gets the Mole rotation so it is always looking away from the wall, depending on its X local position and the wall's curvature (curveCoeff)
    private Quaternion DefineMoleRotation(int xIndex, int yIndex)
    {
        Quaternion lookAngle = new Quaternion();
        lookAngle.eulerAngles = new Vector3(-((((float)yIndex/(rowCount - 1)) * 2) - 1) * (maxAngle * yCurveRatio), ((((float)xIndex/(columnCount - 1)) * 2) - 1) * (maxAngle * xCurveRatio), 0f);
        return lookAngle;
    }

    private IEnumerator WallUpdateCooldown()
    {
        yield return new WaitForSeconds(updateCooldownDuration);

        if(active)
        {
            Clear();
            Enable();
        }
    }
}
