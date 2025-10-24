using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card Game/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public string description;
    public int manaCost;
    public float value;
    public CardType cardType;
    public Sprite artwork;
}