// Show/hide buttons and the informative strength meter for every password field.
// The server still validates all password rules; this script only helps while typing.
(function () {
    document.querySelectorAll("[data-toggle-password]").forEach(function (button) {
        var input = document.getElementById(button.getAttribute("data-toggle-password"));
        if (!input) return;

        button.addEventListener("click", function () {
            var show = input.type === "password";
            input.type = show ? "text" : "password";
            button.textContent = show ? "Ocultar" : "Mostrar";
            button.setAttribute("aria-pressed", show ? "true" : "false");
            button.setAttribute("aria-label", show ? "Ocultar contraseña" : "Mostrar contraseña");
        });
    });

    var levels = [
        { max: 2, text: "Débil", css: "bg-danger", width: 33 },
        { max: 4, text: "Media", css: "bg-warning", width: 66 },
        { max: 5, text: "Fuerte", css: "bg-success", width: 100 }
    ];

    document.querySelectorAll("[data-strength-for]").forEach(function (meter) {
        var input = document.getElementById(meter.getAttribute("data-strength-for"));
        var bar = meter.querySelector(".progress-bar");
        var label = meter.querySelector("[data-strength-label]");
        var items = meter.querySelectorAll("[data-rule]");
        if (!input || !bar || !label) return;

        var rules = {
            length: function (v) { return v.length >= 8; },
            upper: function (v) { return /[A-Z]/.test(v); },
            lower: function (v) { return /[a-z]/.test(v); },
            digit: function (v) { return /\d/.test(v); },
            special: function (v) { return /[^a-zA-Z\d]/.test(v); }
        };

        function update() {
            var value = input.value || "";
            var met = 0;

            Object.keys(rules).forEach(function (key) {
                var ok = rules[key](value);
                if (ok) met++;
                items.forEach(function (item) {
                    if (item.getAttribute("data-rule") === key) {
                        item.classList.toggle("text-success", ok);
                        item.classList.toggle("text-muted", !ok);
                    }
                });
            });

            if (!value) {
                bar.style.width = "0%";
                bar.className = "progress-bar";
                label.textContent = "";
                return;
            }

            var level = levels.find(function (l) { return met <= l.max; });
            bar.style.width = level.width + "%";
            bar.className = "progress-bar " + level.css;
            label.textContent = level.text;
        }

        input.addEventListener("input", update);
        update();
    });
})();
