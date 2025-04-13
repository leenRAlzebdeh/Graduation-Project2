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




