using UnityEngine;

[CreateAssetMenu(menuName = "PokerBot/BotPersonality")]
public class BotPersonality : ScriptableObject
{
    [Range(1, 10)] public int aggression;
    [Range(1, 10)] public int tightness;
    [Range(1, 10)] public int bluffiness;
    public string archetypeName;
}
