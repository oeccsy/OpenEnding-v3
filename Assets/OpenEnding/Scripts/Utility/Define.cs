using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    public enum SceneType
    {
        ConnectScene,
        RomanScene
    }

    public enum ConnectType
    {
        None,
        Server,
        Client
    }

    public enum ConnectStatus
    {
        LeaveParty,
        TryingToJoin,
        JoinParty
    }

    public enum DisplayedFace
    {
        None,
        Head,
        Tail,
        Stand
    }

    public enum CameraIndex
    {
        Main,
        UI
    }
}

namespace Game.GameType.Roman
{
    public enum GameStep
    {
        InitGame,
        SelectCard,
        FlipOrShake,
        ShowCard,
        HideCard,
        GameOver,
        Pause
    }
    
    public enum CardType
    {
        None,
        Star,
        Telescope,
        Artwork,
        Sprout,
        RoleModel,
        Shower,
        PaperPlane,
        H,
        I,
        J
    }
}