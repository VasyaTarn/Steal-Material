using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerObjectReferences : MonoBehaviour
{
    public GameObject model;

    [Header("Range Attack")]
    public Transform projectileSpawnPoint;

    [Header("Plant Objects")]
    public Transform hookshotTransform;
    public Transform summonedEntitySpawnPoint;

    [Header("Basic Objects")]
    public Transform basicMeleePointPosition;

    [Header("Stone Objects")]
    public Transform stoneMeleePointPosition;
    public Transform stoneDefensePointPosition;
    public Transform[] stoneSpecialSmokePositions;
}
