document.addEventListener("DOMContentLoaded", function () {
    // Example data from "database"
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
    document.getElementById("admin-name").textContent = adminData.name;
    document.getElementById("total-cabinets").textContent = adminData.totalCabinets;
    document.getElementById("pending-requests").textContent = adminData.pendingRequests;
    document.getElementById("total-employees").textContent = adminData.totalEmployees;
    document.getElementById("total-reports").textContent = adminData.totalReports;

    // Build table rows
    const cabinetList = document.getElementById("cabinet-list");
    cabinetList.innerHTML = "";
    adminData.cabinets.forEach(cabinet => {
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

    // Dark mode toggle
    const darkModeToggle = document.getElementById("dark-mode-toggle");
    darkModeToggle.addEventListener("click", function () {
        document.body.classList.toggle("dark-mode");
        const icon = darkModeToggle.querySelector("i");
        if (document.body.classList.contains("dark-mode")) {
            icon.classList.remove("fa-moon");
            icon.classList.add("fa-sun");
        } else {
            icon.classList.remove("fa-sun");
            icon.classList.add("fa-moon");
        }
    });
});
document.getElementById("cabinet-management-toggle").addEventListener("click", function () {
    const submenu = document.getElementById("cabinet-submenu");
    submenu.classList.toggle("show");
});

document.getElementById("Employee-management-toggle").addEventListener("click", function () {
    const submenu = document.getElementById("cabinet-submenu");
    submenu.classList.toggle("show");
});
const cabinetToggle = document.getElementById("cabinet-management-toggle");
const cabinetSubmenu = document.getElementById("cabinet-submenu");
const employeeToggle = document.getElementById("Employee-management-toggle");
const employeeSubmenu = document.getElementById("Employee-submenu");

// Track the currently open submenu
let openSubmenu = null;

// Function to close all submenus
function closeAllSubmenus() {
    cabinetSubmenu.classList.remove("show");
    employeeSubmenu.classList.remove("show");
    openSubmenu = null;
}

// Function to toggle a submenu
function toggleSubmenu(toggle, submenu) {
    if (openSubmenu === submenu) {
        // Clicking the same toggle closes it
        closeAllSubmenus();
    } else {
        // Close any open submenu and open the new one
        closeAllSubmenus();
        submenu.classList.add("show");
        openSubmenu = submenu;
    }
}

// Cabinet management toggle
cabinetToggle.addEventListener("click", function (e) {
    e.stopPropagation();
    toggleSubmenu(cabinetToggle, cabinetSubmenu);
});

// Employee management toggle
employeeToggle.addEventListener("click", function (e) {
    e.stopPropagation();
    toggleSubmenu(employeeToggle, employeeSubmenu);
});

// Prevent submenu from closing when clicking inside it
cabinetSubmenu.addEventListener("click", function (e) {
    e.stopPropagation();
});

employeeSubmenu.addEventListener("click", function (e) {
    e.stopPropagation();
});


