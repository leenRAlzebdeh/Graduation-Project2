document.addEventListener("DOMContentLoaded", function () {
    // Example data from "database" (replace with dynamic fetch in production)
   

    // Dark mode toggle with persistence
    const darkModeToggle = document.getElementById("dark-mode-toggle");
    if (darkModeToggle) {
        const darkModeEnabled = localStorage.getItem("darkMode") === "true";
        if (darkModeEnabled) document.body.classList.add("dark-mode");

        darkModeToggle.addEventListener("click", function () {
            document.body.classList.toggle("dark-mode");
            const icon = darkModeToggle.querySelector("i");
            if (document.body.classList.contains("dark-mode")) {
                icon.classList.remove("fa-moon");
                icon.classList.add("fa-sun");
                localStorage.setItem("darkMode", "true");
            } else {
                icon.classList.remove("fa-sun");
                icon.classList.add("fa-moon");
                localStorage.setItem("darkMode", "false");
            }
        });
    }

    // Submenu hover functionality
    let currentOpenMenu = null;

    function closeAllSubmenus() {
        const allSubmenus = document.querySelectorAll(".pretty-submenu");
        allSubmenus.forEach((submenu) => submenu.classList.remove("show"));
        currentOpenMenu = null;
    }

    function openSubmenu(submenu) {
        closeAllSubmenus();
        if (submenu) {
            submenu.classList.add("show");
            currentOpenMenu = submenu;
        }
    }

    // Hover effect for submenu items
    const menuItems = document.querySelectorAll(".menu > li.has-submenu");
    menuItems.forEach((item) => {
        const submenu = item.querySelector(".pretty-submenu");
        if (submenu) {
            // Open submenu on hover
            item.addEventListener("mouseenter", function () {
                openSubmenu(submenu);
            });
            // Close submenu on mouse leave
            item.addEventListener("mouseleave", function () {
                closeAllSubmenus();
            });
        }
    });

    // Close submenus when clicking anywhere else
    document.addEventListener("click", function (e) {
        if (!e.target.closest(".sidebar")) {
            closeAllSubmenus();
        }
    });
});