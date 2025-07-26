using System.Globalization;

namespace MauiCreditCardView.CustomControls;

public struct CreditCardUtils
{
    public static readonly int MaxCvcLength = 3;
    public static readonly int MaxCvcLengthAmex = 4;
    public static readonly int MaxPanLength = 16;
    public static readonly int MaxPanLengthAmericanExpress = 15;
    public static readonly int MaxPanLengthDinersClub = 14;

    private static readonly (CardType type, string icon, string previewIcon, string[] prefix)[] Prefixes =
    {
            (CardType.Mastercard, "ic_mastercard", "MC", new string[] {
                "2221", "2222", "2223", "2224", "2225", "2226",
                "2227", "2228", "2229", "223", "224", "225", "226",
                "227", "228", "229", "23", "24", "25", "26", "270",
                "271", "2720", "50", "51", "52", "53", "54", "55",
                "67" }),
            (CardType.Visa, "ic_visa", "VISA", new string[] { "4" }),
            (CardType.AmericanExpress, "ic_amex", "AMEX", new string[] { "34", "37" }),
            (CardType.UnionPay,"ic_union_pay", "UP", new string[] { "62" }),
            (CardType.Discover,"ic_discover", "Disc", new string[] { "6011", "622", "64", "65" }),
            (CardType.JCB, "ic_jcb", "JCB", new string[] { "35" }),
            (CardType.DinersClub, "ic_diners_club", "DC", new string[] { "300", "301", "302", "303", "304", "305", "309", "36", "38", "39" }),
            (CardType.MIR,"ic_mir", "MNP", new string[] { "2" })
        };

    public static bool IsValidCreditCard(string cardNumber, string expiry, string cvc) => (IsValidCardNumber(cardNumber) && IsValidCardExpiry(expiry) && IsValidCVC(cvc, GetCardType(cardNumber).Type));

    public static bool IsValidCardNumber(string cardNumber)
    {
        try
        {
            cardNumber = GetNumbersOnlyString(cardNumber);

            if (string.IsNullOrEmpty(cardNumber)) return false;
            var network = GetCardType(cardNumber);
            if (network.Type == CardType.Unknown) return false;
            if (!IsValidLength(cardNumber, network.Type)) return false;

            int sum = 0;
            char[] reversedCharacters = cardNumber.Reverse().ToArray();
            for (int idx = 0; idx < reversedCharacters.Length; idx++)
            {
                char element = reversedCharacters[idx];
                if (!int.TryParse(element.ToString(), out int digit)) return false;

                bool isOddIndex = idx % 2 == 1;
                switch (isOddIndex, digit)
                {
                    case (true, 9):
                        sum += 9;
                        break;
                    case (true, int val) when val >= 0 && val <= 8:
                        sum += (digit * 2) % 9;
                        break;
                    default:
                        sum += digit;
                        break;
                }
            }

            return sum % 10 == 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public static bool IsValidCardExpiry(string expiryDate)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(expiryDate)) return false;

            expiryDate = expiryDate.Trim();
            if (expiryDate.IndexOf("/") != 2) return false;

            DateTime parsedExpiryDate = default;
            var parsed = false;

            if (expiryDate.Length == 5) parsed = DateTime.TryParseExact(expiryDate, "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedExpiryDate);
            else if (expiryDate.Length == 7) parsed = DateTime.TryParseExact(expiryDate, "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedExpiryDate);

            if (!parsed) return false;

            DateTime currentDate = DateTime.Now;
            if (parsedExpiryDate.Year < currentDate.Year) return false;
            else if (parsedExpiryDate.Year == currentDate.Year && parsedExpiryDate.Month < currentDate.Month) return false;

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public static bool IsValidCVC(string cvc, CardType network)
    {
        var trimmedCvc = GetNumbersOnlyString(cvc) ?? "";
        return (network == CardType.AmericanExpress && trimmedCvc.Length == MaxCvcLengthAmex) ||
            (trimmedCvc.Length == MaxCvcLength);
    }

    public static (CardType Type, string Icon, string PreviewIcon) GetCardType(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber)) return (CardType.Unknown, "", "");
        foreach (var prefix in Prefixes)
        {
            if (prefix.prefix.Any(x => cardNumber.StartsWith(x))) return (prefix.type, prefix.icon, prefix.previewIcon);
        }

        return (CardType.Unknown, "", "");
    }

    public static bool IsValidLength(string cardNumber, CardType network)
    {
        cardNumber = cardNumber.Trim();

        int cardNumberLength = cardNumber.Length;

        if (string.IsNullOrEmpty(cardNumber) || network == CardType.Unknown) return false;

        switch (network)
        {
            case CardType.AmericanExpress:
                return cardNumberLength == MaxPanLengthAmericanExpress;
            case CardType.DinersClub:
                return cardNumberLength == MaxPanLengthDinersClub;
            default:
                return cardNumberLength == MaxPanLength;
        }
    }

    public static string GetNumbersOnlyString(string formattedNumber)
    {
        if (string.IsNullOrWhiteSpace(formattedNumber))
            return string.Empty;
        try
        {
            return new string(formattedNumber.Where(char.IsDigit).ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }
    public static (string, string, string) FormatCardNumber(string cardNumber)
    {
        var network = GetCardType(cardNumber);

        switch (network.Type)
        {
            case CardType.AmericanExpress: //15 Digits
                return (ApplyMasking(cardNumber, "#### ###### #####"), network.Icon, network.PreviewIcon);
            case CardType.DinersClub: //14 digits
                return (ApplyMasking(cardNumber, "#### ###### ####"), network.Icon, network.PreviewIcon);
            default: //16 digits
                return (ApplyMasking(cardNumber, "#### #### #### ####"), network.Icon, network.PreviewIcon);
        }
    }

    public static string ApplyMasking(string inputText, string mask)
    {
        string formattedNumber = "";
        try
        {
            if (string.IsNullOrEmpty(inputText) || string.IsNullOrEmpty(mask)) return inputText;

            int i = 0;
            foreach (var c in mask)
            {
                if (i == inputText.Length) return formattedNumber;
                if (c == '#') formattedNumber += inputText[i].ToString();
                else
                {
                    formattedNumber += c.ToString();
                    if (inputText[i] != c) formattedNumber += inputText[i];
                }
                i++;
            }
        }
        catch (Exception)
        {
        }

        return formattedNumber;
    }

    public enum CardType
    {
        Unknown,
        Visa,
        Mastercard,
        AmericanExpress,
        Discover,
        JCB,
        DinersClub,
        UnionPay,
        MIR
    }
}
