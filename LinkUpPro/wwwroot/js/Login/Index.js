document.addEventListener("DOMContentLoaded", () => {
    const passwordInput = document.getElementById("Password");
    const toggleButton = document.getElementById("togglePassword");

    if (!passwordInput || !toggleButton) {
        return;
    }

    toggleButton.addEventListener("click", () => {
        const isHidden = passwordInput.type === "password";

        passwordInput.type = isHidden ? "text" : "password";

        toggleButton.textContent = isHidden ? "Ocultar" : "Mostrar";

        toggleButton.setAttribute("aria-pressed", String(isHidden));

        toggleButton.setAttribute("aria-label", isHidden ? "Ocultar contraseña" : "Mostrar contraseña");
    });
});
