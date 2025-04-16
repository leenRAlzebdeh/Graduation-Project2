document.addEventListener("DOMContentLoaded", function () {
    // Example data from "database" (replace with dynamic fetch in production)
    const adminData = {
        name: "Yasmeen Gharaibeh",
        totalCabinets: 150,
        pendingRequests: 5,
        totalEmployees: 20,
        totalReports: 2,
        cabinets: [
            { id: 101, department: "Engineering", owner: "John Doe", status: "Pending" },
            { id: 102, department: "Medicine", owner: "Jane Smith", status: "Active" }
        ]
    };

    // Populate dashboard
    const adminNameElement = document.getElementById("admin-name");
    if (adminNameElement) adminNameElement.textContent = adminData.name;

    const totalCabinetsElement = document.getElementById("total-cabinets");
    if (totalCabinetsElement) totalCabinetsElement.textContent = adminData.totalCabinets;

    const pendingRequestsElement = document.getElementById("pending-requests");
    if (pendingRequestsElement) pendingRequestsElement.textContent = adminData.pendingRequests;

    const totalEmployeesElement = document.getElementById("total-employees");
    if (totalEmployeesElement) totalEmployeesElement.textContent = adminData.totalEmployees;

    const totalReportsElement = document.getElementById("total-reports");
    if (totalReportsElement) totalReportsElement.textContent = adminData.totalReports;

    // Build table rows
    const cabinetList = document.getElementById("cabinet-list");
    if (cabinetList) {
        cabinetList.innerHTML = "";
        adminData.cabinets.forEach((cabinet) => {
            const row = document.createElement("tr");
            row.innerHTML = `
                <td>${cabinet.id}</td>
                <td>${cabinet.department}</td>
                <td>${cabinet.owner}</td>
                <td>${cabinet.status}</td>
                <td>
                    <button class="approve">Approve</button>
                    <button class="deny">Deny</button>
                </td>
            `;
            cabinetList.appendChild(row);
        });
    }

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