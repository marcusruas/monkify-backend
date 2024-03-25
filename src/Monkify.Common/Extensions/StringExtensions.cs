using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Security.Cryptography;

namespace Monkify.Common.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Receives a string and returns the same string, but without accents (such as á, ê, ò, ç...)
    /// </summary>
    /// <param name="text">string to be converted</param>
    /// <returns>the given string, however without accents</returns>
    public static string RemoveAccents(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var stringBuilder = new StringBuilder();

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        foreach (var character in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                stringBuilder.Append(character);
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).Trim();
    }

    /// <summary>
    /// Validates if the passed string contains white spaces (" ")
    /// 
    /// Returns false if the string is null or only contains empty spaces
    /// </summary>
    public static bool HasWhitespaces(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Validates if the passed string contains accents (such as á, ê, ò, ç...). 
    /// 
    /// The method uses the <see cref="RemoveAccents"/> method for comparison.
    ///
    /// Returns false if the string is null or only contains empty spaces
    /// </summary>
    public static bool ContemAcentos(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var formattedText = text.Trim().ToUpper();
        var normalizedText = text.RemoveAccents();

        return normalizedText != formattedText;
    }

    /// <summary>
    /// Converts a string to SHA256
    /// </summary>
    public static string ToSHA256(this string text)
    {
        var shaService = SHA256.Create();
        string hash = string.Empty;
        byte[] bytes = shaService.ComputeHash(Encoding.ASCII.GetBytes(text));
        foreach (byte theByte in bytes)
        {
            hash += theByte.ToString("x2");
        }
        return hash;
    }
}
