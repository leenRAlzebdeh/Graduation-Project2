function searchStudent() {
    const id = document.getElementById("studentIdInput").value;

    fetch('/Supervisor/SearchStudent', {
        method: 'POST',
        body: new URLSearchParams({ id })
    })
        .then(res => res.json())
        .then(data => {
            const studentInfo = document.getElementById("studentInfo");
            const notFoundMsg = document.getElementById("notFoundMsg");

            if (!data.exists) {
                /* studentInfo.style.display = "none";  */ // Hide student info box
                notFoundMsg.style.display = "block"; // Show error message
                notFoundMsg.classList.add("fade-in");
                studentInfo.classList.remove("show");
                studentInfo.style.display = "none";

                setTimeout(() => notFoundMsg.classList.remove("fade-in"), 500);
            } else {
                const s = data.student;

                // document.getElementById("s_id").textContent = s.id;
                document.getElementById("s_name").textContent = s.name;
                document.getElementById("s_email").textContent = s.email;
                document.getElementById("s_department").textContent = s.department;
                document.getElementById("s_location").textContent = s.location;
                document.getElementById("s_lockerId").textContent = s.lockerId || "No Locker";
                document.getElementById("s_blockStatus").textContent = s.isBlocked ? "Blocked" : "Not Blocked";

                notFoundMsg.style.display = "none";  // Hide error message
                studentInfo.classList.remove("show");
                studentInfo.style.display = "block";
                setTimeout(() => studentInfo.classList.add("show"), 10);

                const actionDiv = document.getElementById("blockActionBtn");
                actionDiv.innerHTML = ""; // Reset action button

                const form = document.createElement('form');
                form.method = "POST";
                form.action = "/Supervisor/ToggleBlock";

                const idField = document.createElement('input');
                idField.type = "hidden";
                idField.name = "id";
                idField.value = s.id;

                const blockField = document.createElement('input');
                blockField.type = "hidden";
                blockField.name = "block";
                blockField.value = s.isBlocked ? "false" : "true";

                const button = document.createElement('button');
                button.className = s.isBlocked ? "btn btn-success" : "btn btn-danger";
                button.textContent = s.isBlocked ? "Remove from Block List" : "Add to Block List";

                form.appendChild(idField);
                form.appendChild(blockField);
                form.appendChild(button);
                actionDiv.appendChild(form);
            }
        })
        .catch(error => console.error("Error:", error));





}
function cancelStudentInfo() {
    const studentInfo = document.getElementById("studentInfo");
    studentInfo.classList.remove("show"); // trigger transition out
    setTimeout(() => {
        studentInfo.style.display = "none";
    }, 500); // match the CSS transition duration
}
