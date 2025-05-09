document.addEventListener("DOMContentLoaded", function () {


    var userId = @(userId ?? 0);

    document.getElementById("SupervisorID").value = userId;


    const cabinetIdInput = document.getElementById("CurrentCabinetID");

    cabinetIdInput.addEventListener("blur", function () {
        const cabId = cabinetIdInput.value.trim();
        if (!cabId) return;

        fetch(`/Cabinet/GetCabinet?cabinet_id=${cabId}`)
            .then(response => response.json())
            .then(data => {
                if (data) {
                    cabinetIdInput.classList.remove("is-invalid");
                    document.getElementById("cabinetError").style.display = "none";

                    document.querySelector('input[name="NumberCab"]').value = data.number_cab;
                    document.querySelector('input[name="CurrentLocation"]').value = data.location;
                    document.querySelector('input[name="CurrentDepartment"]').value = data.department_name;
                    document.querySelector('input[name="wing"]').value = data.wing;
                    document.querySelector('input[name="level"]').value = data.level;
                    document.getElementById("submitBtn").disabled = false;
                } else {
                    cabinetIdInput.classList.add("is-invalid");
                    document.getElementById("cabinetError").style.display = "block";

                    document.querySelector('input[name="NumberCab"]').value = "";
                    document.querySelector('input[name="CurrentLocation"]').value = "";
                    document.querySelector('input[name="CurrentDepartment"]').value = "";
                    document.querySelector('input[name="wing"]').value = "";
                    document.querySelector('input[name="level"]').value = "";
                    document.getElementById("submitBtn").disabled = true;
                }
            })
            .catch(error => {
                // console.error("Error fetching cabinet info:", error);
                alert("Failed to fetch cabinet info.");
            });
    });
    cabinetIdInput.addEventListener("input", () => {
        cabinetIdInput.classList.remove("is-invalid");
        document.getElementById("cabinetError").style.display = "none";
    });

});