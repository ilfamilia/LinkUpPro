
document.addEventListener("DOMContentLoaded", () => {
    const currentPassword = document.getElementById("CurrentPassword");
    const newPassword = document.getElementById("NewPassword");
    const confirmPassword = document.getElementById("ConfirmPassword");

    const strengthBar = document.getElementById("passwordStrengthBar");
    const strengthLabel = document.getElementById("passwordStrengthLabel");
    const message = document.getElementById("passwordChangeMessage");
    const submitButton = document.getElementById("profileSubmitButton");

    if (!currentPassword || !newPassword || !confirmPassword ||
        !strengthBar || !strengthLabel || !message || !submitButton) {
        return;
    }

    // Cada botón controla únicamente su propio campo.
    document.querySelectorAll("[data-password-target]")
        .forEach(button => {
            button.addEventListener("click", () => {
                const input = document.getElementById(
                    button.dataset.passwordTarget
                );

                if (!input) {
                    return;
                }

                const isHidden = input.type === "password";

                input.type = isHidden ? "text" : "password";
                button.textContent = isHidden ? "Ocultar" : "Mostrar";
                button.setAttribute("aria-pressed", String(isHidden));
            });
        });

    function calculateStrength(password) {
        const requirements = [
            password.length >= 8,
            /[A-Z]/.test(password),
            /[a-z]/.test(password),
            /[0-9]/.test(password),
            /[^a-zA-Z0-9]/.test(password)
        ];

        return requirements.filter(Boolean).length;
    }

    function updatePasswordFields() {
        const current = currentPassword.value;
        const password = newPassword.value;
        const confirmation = confirmPassword.value;

        const allEmpty = current.length === 0 && password.length === 0 &&
            confirmation.length === 0;

        const strength = calculateStrength(password);
        const percentage = strength * 20;

        strengthBar.classList.remove(
            "w-0",
            "bg-danger",
            "bg-warning",
            "bg-success"
        );

        strengthBar.style.width = `${percentage}%`;
        strengthBar.setAttribute("aria-valuenow", String(percentage));

        if (password.length === 0) {
            strengthLabel.textContent = "Fortaleza: Sin evaluar";
        }
        else if (strength <= 2) {
            strengthLabel.textContent = "Fortaleza: Débil";
            strengthBar.classList.add("bg-danger");
        }
        else if (strength <= 4) {
            strengthLabel.textContent = "Fortaleza: Media";
            strengthBar.classList.add("bg-warning");
        }
        else {
            strengthLabel.textContent = "Fortaleza: Fuerte";
            strengthBar.classList.add("bg-success");
        }

        // Se permite guardar el perfil sin cambiar la contraseña.
        if (allEmpty) {
            submitButton.disabled = false;
            message.textContent = "";
            return;
        }

        const allFilled = current.length > 0 && password.length > 0 &&
            confirmation.length > 0;

        const passwordsMatch = password === confirmation;

        const passwordIsStrong = strength === 5;

        const passwordIsDifferent = password !== current;

        const valid = allFilled && passwordsMatch && passwordIsStrong &&
            passwordIsDifferent;

        submitButton.disabled = !valid;

        if (!allFilled) {
            message.textContent = "Para cambiar la contraseña debes completar los tres campos.";
        }
        else if (!passwordIsStrong) {
            message.textContent = "La nueva contraseña no cumple todos los requisitos.";
        }
        else if (!passwordsMatch) {
            message.textContent = "La nueva contraseña y su confirmación no coinciden.";
        }
        else if (!passwordIsDifferent) {
            message.textContent = "La nueva contraseña debe ser diferente de la actual.";
        }
        else {
            message.textContent = "Los datos de la nueva contraseña son válidos.";
        }
    }

    currentPassword.addEventListener("input", updatePasswordFields);
    newPassword.addEventListener("input", updatePasswordFields);
    confirmPassword.addEventListener("input", updatePasswordFields);

    updatePasswordFields();
});
