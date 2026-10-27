document.addEventListener("DOMContentLoaded", () => {

    const passwordInput = document.getElementById("Password");
    const confirmPasswordInput = document.getElementById("ConfirmPassword");

    const strengthBar = document.getElementById("passwordStrengthBar");
    const strengthLabel = document.getElementById("passwordStrengthLabel");
    const registerButton = document.getElementById("registerSubmitButton");

    const toggleButtons = document.querySelectorAll("[data-password-target]");

    toggleButtons.forEach(button => {
        button.addEventListener("click", () => {
            const targetId = button.dataset.passwordTarget;
            const input = document.getElementById(targetId);

            if (!input) {
                return;
            }

            const isHidden = input.type === "password";

            input.type = isHidden ? "text" : "password";

            button.textContent = isHidden ? "Ocultar" : "Mostrar";
            button.setAttribute("aria-pressed", String(isHidden));
        });
    });

    function getPasswordStrength(password) {
        const requirements = [
            password.length >= 8,
            /[A-Z]/.test(password),
            /[a-z]/.test(password),
            /[0-9]/.test(password),
            /[^a-zA-Z0-9]/.test(password)
        ];

        return requirements.filter(Boolean).length;
    }

    function updatePasswordStrength() {
        const password = passwordInput.value;
        const confirmPassword = confirmPasswordInput.value;

        const strength = getPasswordStrength(password);

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

        const passwordsMatch = confirmPassword.length > 0 && password === confirmPassword;

        registerButton.disabled = !passwordIsStrong || !passwordsMatch;
    }

    passwordInput.addEventListener("input", updatePasswordStrength);

    confirmPasswordInput.addEventListener("input", updatePasswordStrength);

    updatePasswordStrength();
});
