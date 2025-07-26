using System.Globalization;
using System.Windows.Input;

namespace MauiCreditCardView.CustomControls;

public partial class CreditCardEntryView : ContentView
{
    #region Bindable Properties

    public static readonly BindableProperty CardNumberProperty =
        BindableProperty.Create(nameof(CardNumber), typeof(string), typeof(CreditCardEntryView), string.Empty, BindingMode.TwoWay, propertyChanged: OnCardNumberChanged);
    public static readonly BindableProperty ExpireDateProperty =
        BindableProperty.Create(nameof(ExpireDate), typeof(string), typeof(CreditCardEntryView), string.Empty, BindingMode.TwoWay);
    public static readonly BindableProperty CardNameProperty =
    BindableProperty.Create(nameof(CardName), typeof(string), typeof(CreditCardEntryView), string.Empty, BindingMode.TwoWay, propertyChanged: OnCardNameChanged);
    public static readonly BindableProperty CvcNumberProperty =
        BindableProperty.Create(nameof(CvcNumber), typeof(string), typeof(CreditCardEntryView), string.Empty, BindingMode.TwoWay, propertyChanged: OnCvcNumberChanged);
    public static readonly BindableProperty ErrorMessageProperty =
    BindableProperty.Create(nameof(ErrorMessage), typeof(string), typeof(CreditCardEntryView), string.Empty);
    public static readonly BindableProperty AddCardButtonCommandProperty =
        BindableProperty.Create(nameof(AddCardButtonCommand), typeof(ICommand), typeof(CreditCardEntryView));
    public static readonly BindableProperty IsAddCardButtonEnabledProperty =
        BindableProperty.Create(nameof(IsAddCardButtonEnabled), typeof(bool), typeof(CreditCardEntryView), true);

    #endregion

    #region Properties

    public string CardNumber
    {
        get => (string)GetValue(CardNumberProperty);
        set => SetValue(CardNumberProperty, value);
    }
    public string ExpireDate
    {
        get => (string)GetValue(ExpireDateProperty);
        set => SetValue(ExpireDateProperty, value);
    }
    public string CardName
    {
        get => (string)GetValue(CardNameProperty);
        set => SetValue(CardNameProperty, value);
    }
    public string CvcNumber
    {
        get => (string)GetValue(CvcNumberProperty);
        set => SetValue(CvcNumberProperty, value);
    }
    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }
    public ICommand AddCardButtonCommand
    {
        get => (ICommand)GetValue(AddCardButtonCommandProperty);
        set => SetValue(AddCardButtonCommandProperty, value);
    }

    public bool IsAddCardButtonEnabled
    {
        get => (bool)GetValue(IsAddCardButtonEnabledProperty);
        set => SetValue(IsAddCardButtonEnabledProperty, value);
    }

    private int MaxCvcLength => CreditCardUtils.GetCardType(CardNumber).Type == CreditCardUtils.CardType.AmericanExpress
        ? CreditCardUtils.MaxCvcLengthAmex
        : CreditCardUtils.MaxCvcLength;

    #endregion

    #region Variables
    private bool _isUpdatingExpireDate = false;
    private bool _isUpdatingCardNumber = false;
    private bool _isCardValid, _isExpiryValid, _isCvcValid, _isNameValid;
    private static readonly Color ValidColor = Color.FromArgb("#2B762F");
    private static readonly Color ErrorColor = Color.FromArgb("#FF3366");
    private static Color DefaultBorderColor => Application.Current.RequestedTheme == AppTheme.Dark
                ? (Color)Application.Current.Resources["PaleBlue"]
                : (Color)Application.Current.Resources["DarkNavyBlue"];
    #endregion
    public CreditCardEntryView()
    {
        InitializeComponent();
        cardNumberEntry.TextChanged += OnCardNumberTextChanged;
        expireDateEntry.TextChanged += OnExpireDateTextChanged;
        cvcEntry.TextChanged += OnCvcTextChanged;
        addCardButton.Clicked += OnAddCardClicked;
        cardNameEntry.TextChanged += OnCardNameTextChanged;
    }

    #region CardNumber Functions
    private void OnCardNumberTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingCardNumber) return;

        _isUpdatingCardNumber = true;
        try
        {
            var cursorPosition = cardNumberEntry.CursorPosition;
            var result = ProcessCardNumberInput(e.OldTextValue, e.NewTextValue, cursorPosition);
            UpdateCardNumberUI(result.formattedNumber, result.icon, result.previewIcon, result.hasValidPattern);
            try
            {
                cardNumberEntry.CursorPosition = result.newCursorPosition;
            }
            catch
            {
                cardNumberEntry.CursorPosition = result.formattedNumber.Length;
            }
        }
        finally
        {
            _isUpdatingCardNumber = false;
        }
    }

    private (string formattedNumber, string icon, string previewIcon, bool hasValidPattern, int newCursorPosition) ProcessCardNumberInput(
        string oldText, string newText, int cursorPosition)
    {
        if (string.IsNullOrEmpty(newText))
        {
            return (string.Empty, null, null, false, 0);
        }

        var digitsOnly = CreditCardUtils.GetNumbersOnlyString(newText);
        bool isPasteOperation = DetectPasteOperation(oldText, newText);
        var cardType = CreditCardUtils.GetCardType(digitsOnly);
        int maxLength = GetMaxCardLength(cardType.Type);

        if (digitsOnly.Length > maxLength)
        {
            digitsOnly = digitsOnly.Substring(0, maxLength);
        }

        var formattedNumber = FormatCardNumberCorrectly(digitsOnly, cardType.Type);
        bool hasValidPattern = !string.IsNullOrEmpty(cardType.Icon);

        int newCursorPosition = isPasteOperation
            ? formattedNumber.Length
            : CalculateNewCursorPosition(oldText, formattedNumber, cursorPosition, digitsOnly);

        return (formattedNumber, cardType.Icon, cardType.PreviewIcon, hasValidPattern, newCursorPosition);
    }

    private string FormatCardNumberCorrectly(string digitsOnly, CreditCardUtils.CardType cardType)
    {
        if (string.IsNullOrEmpty(digitsOnly))
            return string.Empty;

        var result = new System.Text.StringBuilder();

        int[] spacingPattern = cardType switch
        {
            CreditCardUtils.CardType.AmericanExpress => new[] { 4, 6, 5 },
            CreditCardUtils.CardType.DinersClub => new[] { 4, 6, 4 },
            _ => new[] { 4, 4, 4, 4 }
        };

        int digitIndex = 0;

        for (int groupIndex = 0; groupIndex < spacingPattern.Length && digitIndex < digitsOnly.Length; groupIndex++)
        {
            int groupSize = spacingPattern[groupIndex];
            int digitsToTake = Math.Min(groupSize, digitsOnly.Length - digitIndex);

            result.Append(digitsOnly.Substring(digitIndex, digitsToTake));
            digitIndex += digitsToTake;
            if (digitIndex < digitsOnly.Length && groupIndex < spacingPattern.Length - 1)
            {
                result.Append(' ');
            }
        }

        return result.ToString();
    }

    private bool DetectPasteOperation(string oldText, string newText)
    {
        if (string.IsNullOrEmpty(oldText)) return false;

        var oldDigits = CreditCardUtils.GetNumbersOnlyString(oldText);
        var newDigits = CreditCardUtils.GetNumbersOnlyString(newText);
        int digitDifference = newDigits.Length - oldDigits.Length;

        return digitDifference > 1;
    }

    private int GetMaxCardLength(CreditCardUtils.CardType cardType)
    {
        return cardType switch
        {
            CreditCardUtils.CardType.AmericanExpress => CreditCardUtils.MaxPanLengthAmericanExpress,
            CreditCardUtils.CardType.DinersClub => CreditCardUtils.MaxPanLengthDinersClub,
            _ => CreditCardUtils.MaxPanLength
        };
    }
    private static void OnCardNumberChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CreditCardEntryView)bindable;
        var newNumber = (string)newValue;

        if (!string.IsNullOrEmpty(newNumber))
        {
            var digitsOnly = CreditCardUtils.GetNumbersOnlyString(newNumber);
            var cardType = CreditCardUtils.GetCardType(digitsOnly);
            control.cardLogoImage.Source = !string.IsNullOrEmpty(cardType.Icon) ? cardType.Icon : null;
            control.PreviewCardIcon.Text = !string.IsNullOrEmpty(cardType.PreviewIcon) ? cardType.PreviewIcon : "--";
        }
        else
        {
            control.cardLogoImage.Source = null;
            control.PreviewCardIcon.Text = "--";
        }

        if (!string.IsNullOrEmpty(control.CvcNumber) && control.CvcNumber.Length > control.MaxCvcLength)
        {
            control.CvcNumber = control.CvcNumber[..control.MaxCvcLength];
        }
    }

    #endregion

    #region ExpireDate Functions
    private void OnExpireDateTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingExpireDate) return;

        _isUpdatingExpireDate = true;
        try
        {
            var entry = (Entry)sender;
            var cursorPosition = entry.CursorPosition;
            var result = FormatExpireDate(e.NewTextValue, e.OldTextValue, cursorPosition);

            ExpireDate = result.formattedText;
            entry.Text = result.formattedText;
            UpdateExpireDateDisplay(result.formattedText.AsSpan());
            entry.CursorPosition = Math.Min(result.newCursorPosition, result.formattedText.Length);
        }
        finally
        {
            _isUpdatingExpireDate = false;
        }
    }

    private (string formattedText, int newCursorPosition) FormatExpireDate(string newText, string oldText, int cursorPosition)
    {
        if (string.IsNullOrEmpty(newText))
            return (string.Empty, 0);
        string digits = new string(newText.Where(char.IsDigit).ToArray());

        if (digits.Length > 4)
            digits = digits.Substring(0, 4);

        int oldSlashesBeforeCursor = 0;
        if (!string.IsNullOrEmpty(oldText) && cursorPosition <= oldText.Length)
        {
            oldSlashesBeforeCursor = oldText.Substring(0, cursorPosition).Count(c => c == '/');
        }
        int oldDigitsBeforeCursor = cursorPosition - oldSlashesBeforeCursor;

        if (digits.Length >= 1)
        {
            if (digits[0] > '1')
            {
                digits = "0" + digits.Substring(0, Math.Min(3, digits.Length));
                oldDigitsBeforeCursor++;
            }

            if (digits.Length >= 2)
            {
                if (digits[0] == '1' && digits[1] > '2')
                {
                    digits = "12" + digits.Substring(2, Math.Min(2, digits.Length - 2));
                }
                if (digits[0] == '0' && digits[1] == '0')
                {
                    digits = "01" + digits.Substring(2, Math.Min(2, digits.Length - 2));
                }
            }
        }

        string formattedText = ApplyExpireDateFormat(digits);
        int newCursorPosition;
        bool isAddingText = newText.Length > (oldText?.Length ?? 0);

        if (isAddingText)
        {
            int slashesUpToPosition = 0;
            int digitCount = 0;

            for (int i = 0; i < formattedText.Length && digitCount < oldDigitsBeforeCursor + 1; i++)
            {
                if (formattedText[i] == '/')
                {
                    slashesUpToPosition++;
                }
                else
                {
                    digitCount++;
                }
            }

            newCursorPosition = oldDigitsBeforeCursor + slashesUpToPosition + 1;
            if (digits.Length == 2 && oldDigitsBeforeCursor == 1)
            {
                newCursorPosition++;
            }
        }
        else
        {
            int slashesUpToPosition = 0;
            int digitCount = 0;

            for (int i = 0; i < formattedText.Length && digitCount < oldDigitsBeforeCursor; i++)
            {
                if (formattedText[i] == '/')
                {
                    slashesUpToPosition++;
                }
                else
                {
                    digitCount++;
                }
            }

            newCursorPosition = oldDigitsBeforeCursor + slashesUpToPosition;
        }
        newCursorPosition = Math.Max(0, Math.Min(formattedText.Length, newCursorPosition));

        return (formattedText, newCursorPosition);
    }

    private string ApplyExpireDateFormat(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        var result = new System.Text.StringBuilder();

        if (digits.Length > 0)
            result.Append(digits[0]);
        if (digits.Length > 1)
            result.Append(digits[1]);

        if (digits.Length > 2)
            result.Append('/');

        if (digits.Length > 2)
            result.Append(digits[2]);
        if (digits.Length > 3)
            result.Append(digits[3]);

        return result.ToString();
    }

    #endregion

    #region CVC Functions

    private void OnCvcTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!(sender is Entry entry)) return;

        var digits = new string(e.NewTextValue?.Where(char.IsDigit).ToArray());
        if (digits.Length > MaxCvcLength)
        {
            digits = digits[..MaxCvcLength];
        }
        if (digits != e.NewTextValue)
        {
            entry.Text = digits;
            entry.CursorPosition = digits.Length;
        }
    }

    private static void OnCvcNumberChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CreditCardEntryView)bindable;
        var newCvc = (string)newValue;
        var digits = new string(newCvc?.Where(char.IsDigit).ToArray());

        if (digits.Length > control.MaxCvcLength)
        {
            control.CvcNumber = digits[..control.MaxCvcLength];
        }
    }

    #endregion

    #region Card Holder Name
    private static void OnCardNameChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CreditCardEntryView)bindable;
        control.UpdateCardNameDisplay((string)newValue);
    }
    private void OnCardNameTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateCardNameDisplay(cardNameEntry.Text);
    }
    private void UpdateCardNameDisplay(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            PreviewCardName.Text = "YOUR NAME";
            return;
        }
        Span<char> buffer = stackalloc char[name.Length];
        int length = 0;

        foreach (var c in name.AsSpan())
        {
            if (char.IsLetter(c) || char.IsWhiteSpace(c))
            {
                buffer[length++] = char.ToUpperInvariant(c);
                if (length >= 20) break;
            }
        }
        PreviewCardName.Text = length > 0 ? new string(buffer[..length]) : "YOUR NAME";
    }
    #endregion

    #region Common Function
    private int CalculateNewCursorPosition(string oldText, string newText, int oldCursorPosition, string digitsOnly)
    {
        if (string.IsNullOrEmpty(oldText) || string.IsNullOrEmpty(newText))
            return newText.Length;

        int digitsBeforeCursor = 0;
        int actualCursorPos = Math.Min(oldCursorPosition, oldText.Length);

        for (int i = 0; i < actualCursorPos; i++)
        {
            if (char.IsDigit(oldText[i]))
                digitsBeforeCursor++;
        }
        int digitsSeen = 0;
        for (int i = 0; i < newText.Length; i++)
        {
            if (char.IsDigit(newText[i]))
            {
                digitsSeen++;
                if (digitsSeen > digitsBeforeCursor)
                    return i;
            }
        }

        return newText.Length;
    }

    #endregion

    #region VisualStates
    private void OnAddCardClicked(object sender, EventArgs e)
    {
        if (ValidateAll())
        {
            AddCardButtonCommand?.Execute(null);
        }
    }
    internal bool ValidateAll()
    {
        _isCardValid = CreditCardUtils.IsValidCardNumber(CardNumber);
        _isExpiryValid = CreditCardUtils.IsValidCardExpiry(ExpireDate);
        _isCvcValid = CreditCardUtils.IsValidCVC(CvcNumber,
                     CreditCardUtils.GetCardType(CardNumber).Type);
        _isNameValid = !String.IsNullOrWhiteSpace(cardNameEntry.Text);

        UpdateVisualStates();
        return _isCardValid && _isExpiryValid && _isCvcValid && _isNameValid;
    }
    private void UpdateVisualStates()
    {
        cardNumberBorder.Stroke = _isCardValid ? ValidColor : ErrorColor;
        expiryDateBorder.Stroke = _isExpiryValid ? ValidColor : ErrorColor;
        cvcBorder.Stroke = _isCvcValid ? ValidColor : ErrorColor;
        cardNameBorder.Stroke = _isNameValid ? ValidColor : ErrorColor;

        ErrorMessage = (_isCardValid && _isExpiryValid && _isCvcValid && _isNameValid)
            ? string.Empty
            : "Invalid card details";
    }
    private void UpdateCardNumberUI(string formattedNumber, string icon, string previewIcon, bool hasValidPattern)
    {
        cardLogoImage.Source = hasValidPattern && !string.IsNullOrEmpty(formattedNumber) ? icon : null;
        PreviewCardIcon.Text = hasValidPattern && !string.IsNullOrEmpty(formattedNumber) ? previewIcon : "--";
        CardNumber = formattedNumber;
        UpdateCardNumberDisplay(formattedNumber);
    }
    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry)
        {
            if (entry == cardNumberEntry)
            {
                _isCardValid = CreditCardUtils.IsValidCardNumber(CardNumber);
                cardNumberBorder.Stroke = _isCardValid ? ValidColor : ErrorColor;
            }
            else if (entry == expireDateEntry)
            {
                _isExpiryValid = CreditCardUtils.IsValidCardExpiry(ExpireDate);
                expiryDateBorder.Stroke = _isExpiryValid ? ValidColor : ErrorColor;
            }
            else if (entry == cvcEntry)
            {
                var cardType = CreditCardUtils.GetCardType(CardNumber).Type;
                _isCvcValid = CreditCardUtils.IsValidCVC(CvcNumber, cardType);
                cvcBorder.Stroke = _isCvcValid ? ValidColor : ErrorColor;
            }
            else if (entry == cardNameEntry)
            {
                cardNameBorder.Stroke = String.IsNullOrWhiteSpace(cardNameEntry.Text) ? ErrorColor : ValidColor;
            }
        }
    }
    private void OnEntryFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry)
        {
            if (entry == cardNumberEntry)
            {
                cardNumberBorder.Stroke = DefaultBorderColor;
            }
            else if (entry == expireDateEntry)
            {
                expiryDateBorder.Stroke = DefaultBorderColor;
            }
            else if (entry == cvcEntry)
            {
                cvcBorder.Stroke = DefaultBorderColor;
            }
            else if (entry == cardNameEntry)
            {
                cardNameBorder.Stroke = DefaultBorderColor;
            }
        }
    }
    #endregion


    #region Preview Card
    private void UpdateCardNumberDisplay(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber))
        {
            PreviewCardNumber.Text = "•••• •••• •••• ••••";
            return;
        }

        var digitsOnly = CreditCardUtils.GetNumbersOnlyString(cardNumber);
        var cardType = CreditCardUtils.GetCardType(digitsOnly);

        int[] spacingPattern = cardType.Type switch
        {
            CreditCardUtils.CardType.AmericanExpress => new[] { 4, 6, 5 },
            CreditCardUtils.CardType.DinersClub => new[] { 4, 6, 4 },
            _ => new[] { 4, 4, 4, 4 }
        };

        var result = new System.Text.StringBuilder();
        int digitIndex = 0;

        for (int groupIndex = 0; groupIndex < spacingPattern.Length; groupIndex++)
        {
            int groupSize = spacingPattern[groupIndex];

            for (int i = 0; i < groupSize; i++)
            {
                if (digitIndex < digitsOnly.Length)
                {
                    result.Append(digitsOnly[digitIndex]);
                }
                else
                {
                    result.Append('•');
                }
                digitIndex++;
            }

            if (groupIndex < spacingPattern.Length - 1)
            {
                result.Append(' ');
            }
        }
        PreviewCardNumber.Text = result.ToString();
    }
    private void UpdateExpireDateDisplay(ReadOnlySpan<char> expireDate)
    {
        Span<char> buffer = stackalloc char[5];
        int position = 0;
        for (int i = 0; i < 2; i++)
        {
            buffer[position++] = i < expireDate.Length && char.IsDigit(expireDate[i])
                ? expireDate[i]
                : 'M';
        }

        buffer[position++] = '/';
        for (int i = 2; i < 4; i++)
        {
            int sourcePos = i + (expireDate.Length > 2 && expireDate[2] == '/' ? 1 : 0);
            buffer[position++] = sourcePos < expireDate.Length && char.IsDigit(expireDate[sourcePos])
                ? expireDate[sourcePos]
                : 'Y';
        }

        PreviewExpireDate.Text = new string(buffer[..5]);
    }
    #endregion

}
