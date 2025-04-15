// Sidebar submenu toggle
document.getElementById("cabinet-management-toggle")?.addEventListener("click", function () {
    const submenu = document.getElementById("cabinet-submenu");
    submenu.classList.toggle("show");
});

// Dark Mode Toggle
document.getElementById("dark-mode-toggle")?.addEventListener("click", function () {
    document.body.classList.toggle("dark-mode");
    const icon = this.querySelector("i");
    icon.classList.toggle("fa-moon");
    icon.classList.toggle("fa-sun");
});

// Update location field when department is selected
document.querySelectorAll('select[id^="departmentName-"]').forEach(select => {
    select.addEventListener('change', function () {
        const selectedOption = this.options[this.selectedIndex];
        const locationField = document.getElementById(`departmentLocation-${this.id.split('-')[1]}`);
        if (selectedOption.dataset.location) {
            locationField.value = selectedOption.dataset.location;
        } else {
            locationField.value = '';
        }
    });
});

// Form validation for modals
(function () {
    'use strict'

    // Fetch all the forms we want to apply validation to
    var forms = document.querySelectorAll('.needs-validation')

    // Loop over them and prevent submission
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (!form.checkValidity()) {
                    event.preventDefault()
                    event.stopPropagation()
                }

                form.classList.add('was-validated')
            }, false)
        })
})()