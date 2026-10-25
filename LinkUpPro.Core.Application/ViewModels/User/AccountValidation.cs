namespace LinkUpPro.Core.Application.ViewModels.User
{
    // Shared patterns so registration, reset and profile apply exactly the same server-side rules.
    public static class AccountValidation
    {
        public const string PhonePattern = @"^(809|829|849)-\d{3}-\d{4}$";
        public const string PhoneMessage = "Debe ingresar un número telefónico válido de República Dominicana.";
        public const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$";
        public const string PasswordMessage = "La contraseña debe tener al menos ocho caracteres, una letra mayúscula, una letra minúscula, un número y un carácter especial.";
    }
}
