document.addEventListener("DOMContentLoaded", () => {
    const passwordInput = document.getElementById("Password");
    const confirmPasswordInput = document.getElementById("ConfirmPassword");

    const strengthBar = document.getElementById("passwordStrengthBar");
    const strengthLabel = document.getElementById("passwordStrengthLabel");
    const submitButton = document.getElementById("resetPasswordSubmitButton");

    if (!passwordInput || !confirmPasswordInput || !strengthBar || !strengthLabel || !submitButton) {
        return;
    }

    // Mostrar y ocultar las contraseñas.
    document.querySelectorAll("[data-password-target]")
        .forEach(button => {
            button.addEventListener("click", () => {
                const input = document.getElementById(button.dataset.passwordTarget);

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

    function updateStrength() {
        const password = passwordInput.value;
        const confirmation = confirmPasswordInput.value;

        const strength = calculateStrength(password);

        strengthBar.classList.remove(
            "w-0",
            "bg-danger",
            "bg-warning",
            "bg-success"
        );

        const percentage = strength * 20;

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

        const passwordIsStrong = strength === 5;
        const passwordsMatch = confirmation.length > 0 && password === confirmation;

        submitButton.disabled = !passwordIsStrong || !passwordsMatch;
    }

    passwordInput.addEventListener("input", updateStrength);
    confirmPasswordInput.addEventListener("input", updateStrength);

    updateStrength();
});