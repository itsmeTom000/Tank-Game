using UnityEngine;

// 1. 'static' means this class exists globally in the background
public static class NameGenerator 
{
    // 2. 'readonly' ensures these lists can never accidentally be overwritten
    private static readonly string[] _shortWords = { 
        "Neo", "Fox", "Rex", "Bot", "Ash", 
        "Zed", "Jax", "Roc", "Nyx", "Sly",
        "Zod", "Max", "Dax", "Vex", "Ron"
    };

    // 3. 'static' allows you to call this method from anywhere
    public static string GenerateShortName()
    {
        string randomWord = _shortWords[Random.Range(0, _shortWords.Length)];
        int randomNumber = Random.Range(10, 100);

        return $"{randomWord}{randomNumber}"; 
    }
}