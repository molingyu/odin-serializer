using System.Text.RegularExpressions;

namespace OdinSerializer.Utilities.Wrapper;

public enum RichTextType
{
    Unity,
    Godot,
    Unknown
}

public static class RichText
{
    public static RichTextType IdentifyRichText(string text)
    {

        if (Regex.IsMatch(text, @"<b>|<i>|<color=.*?>", RegexOptions.IgnoreCase))
        {
            return RichTextType.Unity;
        }

        if (Regex.IsMatch(text, @"\[b\]|\[i\]|\[color=.*?\]", RegexOptions.IgnoreCase))
        {
            return RichTextType.Godot;
        }

        return RichTextType.Unknown;
    }

    public static string ConvertGodotToUnity(string godotText)
    {
        string unityText = Regex.Replace(godotText, @"\[b\]", "<b>", RegexOptions.IgnoreCase);
        unityText = Regex.Replace(unityText, @"\[/b\]", "</b>", RegexOptions.IgnoreCase);

        unityText = Regex.Replace(unityText, @"\[font_size\]", "<size>", RegexOptions.IgnoreCase);
        unityText = Regex.Replace(unityText, @"\[/font_size\]", "</size>", RegexOptions.IgnoreCase);

        unityText = Regex.Replace(unityText, @"\[i\]", "<i>", RegexOptions.IgnoreCase);
        unityText = Regex.Replace(unityText, @"\[/i\]", "</i>", RegexOptions.IgnoreCase);

        unityText = Regex.Replace(unityText, @"\[color=(.*?)\]", "<color=$1>", RegexOptions.IgnoreCase);
        unityText = Regex.Replace(unityText, @"\[/color\]", "</color>", RegexOptions.IgnoreCase);

        return unityText;
    }

    public static string ConvertUnityToGodot(string unityText)
    {
        string godotText = Regex.Replace(unityText, @"<b>", "[b]", RegexOptions.IgnoreCase);
        godotText = Regex.Replace(godotText, @"</b>", "[/b]", RegexOptions.IgnoreCase);

        godotText = Regex.Replace(godotText, @"<size>", "[font_size]", RegexOptions.IgnoreCase);
        godotText = Regex.Replace(godotText, @"</size>", "[/font_size]", RegexOptions.IgnoreCase);

        godotText = Regex.Replace(godotText, @"<i>", "[i]", RegexOptions.IgnoreCase);
        godotText = Regex.Replace(godotText, @"</i>", "[/i]", RegexOptions.IgnoreCase);

        godotText = Regex.Replace(godotText, @"<color=(.*?)>", "[color=$1]", RegexOptions.IgnoreCase);
        godotText = Regex.Replace(godotText, @"</color>", "[/color]", RegexOptions.IgnoreCase);

        return godotText;
    }
}