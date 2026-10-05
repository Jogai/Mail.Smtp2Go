using System.Globalization;
using System.Text;

namespace Scott.Mail.Smtp2Go.Generators;

/// <summary>
/// <c>JsonNamingPolicy.SnakeCaseLower</c>, re-implemented because the generator cannot load System.Text.Json: a separator before an upper-case
/// letter that follows a lower-case letter or digit, or that starts a lower-case run after other upper-case letters (<c>ABCDef</c> becomes
/// <c>abc_def</c>); spaces become one separator; other characters pass through. The unit tests compare it with the real policy.
/// </summary>
internal static class SnakeCase
{
    private enum State
    {
        NotStarted,
        LowercaseLetterOrDigit,
        UppercaseLetter,
        SpaceSeparator,
    }

    public static string Convert(string name)
    {
        StringBuilder builder = new(name.Length + 4);
        State state = State.NotStarted;
        for (int i = 0; i < name.Length; i++)
        {
            char current = name[i];
            switch (CharUnicodeInfo.GetUnicodeCategory(current))
            {
                case UnicodeCategory.UppercaseLetter:
                    switch (state)
                    {
                        case State.LowercaseLetterOrDigit:
                        case State.SpaceSeparator:
                            builder.Append('_');
                            break;
                        case State.UppercaseLetter:
                            if (i + 1 < name.Length && char.IsLower(name[i + 1]))
                            {
                                builder.Append('_');
                            }

                            break;
                    }

                    builder.Append(char.ToLowerInvariant(current));
                    state = State.UppercaseLetter;
                    break;

                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    if (state == State.SpaceSeparator)
                    {
                        builder.Append('_');
                    }

                    builder.Append(current);
                    state = State.LowercaseLetterOrDigit;
                    break;

                case UnicodeCategory.SpaceSeparator:
                    if (state != State.NotStarted)
                    {
                        state = State.SpaceSeparator;
                    }

                    break;

                default:
                    builder.Append(current);
                    state = State.NotStarted;
                    break;
            }
        }

        return builder.ToString();
    }
}
