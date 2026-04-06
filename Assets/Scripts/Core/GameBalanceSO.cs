using UnityEngine;

[CreateAssetMenu(fileName = "GameBalance", menuName = "STRATUM/GameBalance")]
public class GameBalanceSO : ScriptableObject
{
    [Header("Attack Damage per Row (Row1~5)")]
    public int[] dmgTable = { 1, 2, 4, 6, 10 };

    [Header("Shield Amount per Row (Row1~5)")]
    public int[] shieldTable = { 1, 2, 3, 4, 6 };

    [Header("Lock Count per Row (Row1~5)")]
    public int[] lockTable = { 1, 2, 3, 4, 5 };

    [Header("Just Guard")]
    [Tooltip("ATK vs DEF 同時発動時の反射ダメージ倍率")]
    public int justGuardMultiplier = 2;

    [Header("Player")]
    public int initialHP = 20;
    public int maxResourceCapacity = 6;
}
