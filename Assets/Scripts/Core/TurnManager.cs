using System;
using UnityEngine;

public class TurnManager
{
    public GamePhase currentPhase { get; private set; } = GamePhase.AcquireP1;
    public int turnCount { get; private set; } = 1;
    public event Action<GamePhase> OnPhaseChanged;

    // 最初の先手プレイヤー（0 or 1）。ゲーム開始時にランダム決定
    public int initialFirstPlayer { get; private set; } = 0;

    // 現在ターンの先手プレイヤーインデックス
    // 奇数ターン = initialFirstPlayer、偶数ターン = 相手
    public int FirstPlayerIndex  => (turnCount % 2 == 1) ? initialFirstPlayer : 1 - initialFirstPlayer;
    public int SecondPlayerIndex => 1 - FirstPlayerIndex;

    public void Initialize(int firstPlayer)
    {
        initialFirstPlayer = firstPlayer;
        Debug.Log($"[TurnManager] Turn1 first player: Player{firstPlayer + 1}");
    }

    public void NextPhase()
    {
        switch (currentPhase)
        {
            case GamePhase.AcquireP1: SetPhase(GamePhase.AcquireP2); break;
            case GamePhase.AcquireP2: SetPhase(GamePhase.AssignP1);  break;
            case GamePhase.AssignP1:  SetPhase(GamePhase.AssignP2);  break;
            case GamePhase.AssignP2:  SetPhase(GamePhase.Resolve);   break;
            case GamePhase.Resolve:   turnCount++; SetPhase(GamePhase.AcquireP1); break;
        }
    }

    public void SetGameOver() => SetPhase(GamePhase.GameOver);

    void SetPhase(GamePhase p)
    {
        currentPhase = p;
        OnPhaseChanged?.Invoke(p);
        Debug.Log($"[Phase] {p}  Turn:{turnCount}  First:Player{FirstPlayerIndex + 1}");
    }
}
