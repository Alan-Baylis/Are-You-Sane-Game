using UnityEngine;
using UnityStandardAssets.ImageEffects;

public class PlayerEventsUI : PlayerComponent
{
    [SerializeField]
    private CanvasManager m_Canvas;

    private MotionBlur m_MotionBlur;
    private TiltShift m_TiltShift;
    private Vortex m_Vortex;

    private static float m_MotionBlurIncrement;
    private static float m_VortexIncrement;
    private float m_VortexAccumulation;

    public static void EnableVortexEffect(int gameSeconds)
    {
        Camera.main.GetComponent<Vortex>().enabled = true;
        m_VortexIncrement = GameConstants.InsanityVortexCap / gameSeconds;
    }

    public void IncreaseVortex()
    {
        m_VortexAccumulation += m_VortexIncrement;        
        m_Vortex.SetAngleFromCurve(m_VortexAccumulation, GameConstants.InsanityVortexCap);
    }

    public static void SetMotionPerSecond(float amount)
    {
        m_MotionBlurIncrement = amount;
    }

    public void IncreaseMotionBlur()
    {
        m_MotionBlur.blurAmount += m_MotionBlurIncrement;
    }

    public override void Start()
    {
        m_MotionBlur    = Camera.main.GetComponent<MotionBlur>();
        m_TiltShift     = Camera.main.GetComponent<TiltShift>();
        m_Vortex        = Camera.main.GetComponent<Vortex>();
    }


    public void ShowGameOverUI(EndCondition condition)
    {
        m_Canvas.UI_Game.Toggle(false);
        m_Canvas.UI_GameOver.Toggle(true);
        m_Canvas.UI_GameOver.ShowDeathUI(condition);
    }

    public void PromptDoorCloseMessage()
    {
        m_Canvas.UI_Game.Messenger.ShowInteractionMessage(GameMesseage.DoorClose);
    }

    public void PromptDoorMessage()
    {
        // Maybe move this call to the Game UI - unsure rathar than miss the layer
        m_Canvas.UI_Game.Messenger.ShowInteractionMessage(GameMesseage.DoorPrompt);        
    }

    public void LockedDoorMessage()
    {
        // Maybe move this call to the Game UI - unsure rathar than miss the layer
        m_Canvas.UI_Game.Messenger.ShowInteractionMessage(GameMesseage.DoorLocked, 1.4f);
    }

    public void ClearInteractionMessage()
    {
        m_Canvas.UI_Game.Messenger.ClearInteractionMessage();
    }

    public void ResetTime()
    {
        m_Canvas.UI_Game.GameTime.ResetTimer();
    }


}
