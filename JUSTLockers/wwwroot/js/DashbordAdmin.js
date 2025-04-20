
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
            const cabinetNumberInput = document.getElementById("pending-requests");
            if (cabinetNumberInput) {
                const lastCabinetNumber = parseInt(data);
                cabinetNumberInput.textContent = lastCabinetNumber || "1";
            }
        })
        .catch(() => {
            const cabinetNumberInput = document.getElementById("pending-requests");
            if (cabinetNumberInput) {
                cabinetNumberInput.value = "Error fetching pending requests number";
            }
        });

    fetch('/Dashboard/GetReportsNumberJson')
        .then(response => response.json())
        .then(data => {
            const cabinetNumberInput = document.getElementById("total-reports");
            if (cabinetNumberInput) {
                const lastCabinetNumber = parseInt(data);
                cabinetNumberInput.textContent = lastCabinetNumber || "1";
            }
        })
        .catch(() => {
            const cabinetNumberInput = document.getElementById("total-reports");
            if (cabinetNumberInput) {
                cabinetNumberInput.value = "Error fetching total reports number";
            }
        });

    fetch('/Dashboard/GetNameJson')
        .then(response => response.json())
        .then(data => {
            const adminNameElement = document.getElementById("admin-name");
            if (adminNameElement) {
                adminNameElement.textContent = data || "Unknown User";
            }
        })
        .catch(() => {
            const adminNameElement = document.getElementById("admin-name");
            if (adminNameElement) {
                adminNameElement.textContent = "Error fetching admin name";
            }
        });





});