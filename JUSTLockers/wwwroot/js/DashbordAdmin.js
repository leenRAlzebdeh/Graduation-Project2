
document.addEventListener("DOMContentLoaded", function () {
    // Fetching the total number of cabinets
    fetch('/Dashboard/GetLastCabinetNumberJson')
        .then(response => response.json())
        .then(data => {
            const cabinetNumberInput = document.getElementById("total-cabinets");
            if (cabinetNumberInput) {
                const lastCabinetNumber = parseInt(data);
                cabinetNumberInput.textContent = lastCabinetNumber || "1";
            }
        })
        .catch(() => {
            const cabinetNumberInput = document.getElementById("total-cabinets");
            if (cabinetNumberInput) {
                cabinetNumberInput.value = "Error fetching cabinet number";
            }
        });


    fetch('/Dashboard/GetsupervisorNumberJson')
        .then(response => response.json())
        .then(data => {
            const cabinetNumberInput = document.getElementById("total-employees");
            if (cabinetNumberInput) {
                const lastCabinetNumber = parseInt(data);
                cabinetNumberInput.textContent = lastCabinetNumber || "1";
            }
        })
        .catch(() => {
            const cabinetNumberInput = document.getElementById("total-employees");
            if (cabinetNumberInput) {
                cabinetNumberInput.value = "Error fetching employees number";
            }
        });


    fetch('/Dashboard/GetPendingRequestsNumberJson')
        .then(response => response.json())
        .then(data => {
            const PendingRequestsNumberInput = document.getElementById("pending-requests");
            if (PendingRequestsNumberInput) {
                const PendingRequestsNumber = parseInt(data);
                PendingRequestsNumberInput.textContent = PendingRequestsNumber||0 ;
            }
        })
        .catch(() => {
            const PendingRequestsNumberInput = document.getElementById("pending-requests");
            if (PendingRequestsNumberInput) {
                PendingRequestsNumberInput.value = "Error fetching pending requests number";
            }
        });

    fetch('/Dashboard/GetReportsNumberJson')
        .then(response => response.json())
        .then(data => {
            const ReportsNumberInput = document.getElementById("total-reports");
            if (ReportsNumberInput) {
                const tReportsNumber = parseInt(data);
               ReportsNumberInput.textContent = tReportsNumber ;
            }
        })
        .catch(() => {
            const ReportsNumberInput = document.getElementById("total-reports");
            if (ReportsNumberInput) {
                ReportsNumberInput.value = "Error fetching total reports number";
            }
        });

    fetch('/Dashboard/GetNameJson')
        .then(response => response.json())
        .then(data => {
            const adminNameElement = document.getElementById("admin-name");
            const name = document.getElementById("name");
            if (adminNameElement) {
                adminNameElement.textContent = data || "Unknown User";
                name.innerHTML = data || "Unknown User";
            }
        })
        .catch(() => {
            const adminNameElement = document.getElementById("admin-name");
            if (adminNameElement) {
                adminNameElement.textContent = "Error fetching admin name";
            }
        });





});