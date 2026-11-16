public static class ElementConverter
{
    public static EElements ToPlayerElement(Element element)
    {
        return element switch
        {
            Element.Fire => EElements.Fire,
            Element.Ice => EElements.Ice,
            Element.Water => EElements.Water,
            Element.Air => EElements.Air,
            Element.Earth => EElements.Stone,
            Element.Lightning => EElements.Lightning,
            Element.Arcane => EElements.Arcane,
            Element.Void => EElements.Void,
            Element.Light => EElements.Light,
            _ => EElements.None
        };
    }

}
