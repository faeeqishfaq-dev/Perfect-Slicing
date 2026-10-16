namespace FoodSlicer
{
    public enum FoodType
    {
        // Fruits
        Apple = 0,
        Banana = 1,
        Watermelon = 2,
        Lemon = 3,
        Tomato = 4,

        // Vegetables
        Carrot = 5,
        BellPepper = 6,
        Potato = 7,

        // Bakery & Desserts
        ChocolateCake = 8,
        CreamCake = 9,
        SwissRoll = 10,
        Pie = 11,
        Bread = 12
    }

    public enum WrongFoodPenaltyMode
    {
        None,
        LoseScore,
        ResetCombo,
        FailLevel
    }

    public enum BoardHitMode
    {
        Penalty,
        ResetCombo,
        FailLevel
    }

    public enum MissPenaltyMode
    {
        None,
        ResetCombo,
        LoseScore,
        FailIfTarget
    }

    public enum KnifeInputMode
    {
        Tap,
        HoldRepeat
    }

    public enum GameState
    {
        Menu,
        Loading,
        Playing,
        Paused,
        LevelComplete,
        LevelFailed
    }
}
