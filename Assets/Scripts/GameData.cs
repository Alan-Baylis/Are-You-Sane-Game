using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

/// <summary>
/// Values signify the allowed hours of survival from madness
/// </summary>
public enum GameDifficulty 
{    
    /// <summary>
    /// Insane AI, cheating and no clock/tricking clock
    /// </summary>
    Psychotic   = 3,

    /// <summary>
    /// Difficult AI
    /// </summary>
    Hard        = 4,

    /// <summary>
    /// Moderate gameplay
    /// </summary>
    Medium      = 5,

    /// <summary>
    /// Easy peezy
    /// </summary>
    Easy        = 6
}

/// <summary>
/// DO THIS MUCH LATER - POLISH CODE WORK INTO VALUES CLASS - REMOVE START FUNCITONS FOR EVERYTHING AND CONFIGURE MONOBEHAVIOUR RESETS
/// </summary>
public struct GameValues
{
    public float m_InsanityRColourAlphaInc;
    public float m_InsanityBColourAlphaInc;
}

public static class GameData
{
    
    public static GameDifficulty Difficulty     { get { return m_Difficulty; } }
    public static bool Running                  { get { return m_Running; } }
    public static GameValues Values             { get { return m_Values; } }

    private static GameDifficulty m_Difficulty  = GameDifficulty.Easy;
    private static bool m_Running               = true;
    private static GameValues m_Values;
   

    public static void SetDifficulty(GameDifficulty difficulty)
    {
        m_Difficulty = difficulty;
        int iDifficulty = (int)difficulty;

        int rGameSeconds = iDifficulty * 60;

        // These should be done on respective start as the difficulty should be set before loading the scene
        PlayerEventsUI.SetMotionPerSecond(GameConstants.MotionBlurSpread / rGameSeconds);
        UIInsanity.SetPerSecondIncrements((GameConstants.InsanityBackgroundAlphaCap / rGameSeconds), (GameConstants.InsanityFocalAlphaCap / rGameSeconds));

        if (difficulty == GameDifficulty.Psychotic)
        {
            PlayerEventsUI.EnableVortexEffect(rGameSeconds);
        } 


    }


    public static void StartGame()  { m_Running = true; }
    public static void EndGame()    { m_Running = false; }

}

public static class GameTag
{
    public const string Player      = "Player";
    public const string Annie       = "AnnieAI";
    public const string Door        = "Door";
    public const string Material    = "Material";
}

public static class GameConstants
{
    public const float InsanityFocalAlphaCap = 100f;
    public const float InsanityBackgroundAlphaCap = 40f;
    public const float MotionBlurSpread = 0.6f;
    public const float InsanityVortexCap = 50f;
}

