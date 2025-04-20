
    document.addEventListener("DOMContentLoaded", function () {
            const empIdInput = document.getElementById("Id");
    const nameInput = document.getElementById("Name");
    const emailInput = document.getElementById("Email");
    const locationSelect = document.getElementById("Location");
    const departmentSelect = document.getElementById("DepartmentName");
    const departmentContainer = document.getElementById("department-container");
    const form = document.getElementById("supervisorForm");
    const idError = document.getElementById("idError");
    const departmentError = document.getElementById("departmentError");
    const messageContainer = document.getElementById("messageContainer");

    departmentContainer.style.display = "none";

    function showMessage(type, text) {
                const message = document.createElement("div");
    message.className = `message-box ${type}`;

    const messageText = document.createElement("span");
    messageText.textContent = text;
    message.appendChild(messageText);

    const closeBtn = document.createElement("button");
    closeBtn.className = "close-btn";
    closeBtn.innerHTML = "×";
                closeBtn.addEventListener("click", () => {
        message.classList.remove('show');
                    setTimeout(() => message.remove(), 300);
                });
    message.appendChild(closeBtn);

    messageContainer.appendChild(message);

                setTimeout(() => message.classList.add('show'), 50);

                const autoDismiss = setTimeout(() => {
        message.classList.remove('show');
                    setTimeout(() => message.remove(), 300);
                }, 5000);

                message.addEventListener('mouseenter', () => {
        clearTimeout(autoDismiss);
                });

                message.addEventListener('mouseleave', () => {
        setTimeout(() => {
            message.classList.remove('show');
            setTimeout(() => message.remove(), 300);
        }, 3000);
                });
            }

    @if (TempData["SuccessMessage"] != null)
    {
        <text>
                        setTimeout(() => {
                showMessage('success', '@Html.Raw(TempData["SuccessMessage"])');
                        }, 300);
        </text>
    }
    @if (TempData["ErrorMessage"] != null)
    {
        <text>
                        setTimeout(() => {
                showMessage('error', '@Html.Raw(TempData["ErrorMessage"])');
                        }, 300);
        </text>
    }

    empIdInput.addEventListener("input", function() {
        this.value = this.value.replace(/[^0-9]/g, '');
            });

    empIdInput.addEventListener("change", async function () {
                const empId = this.value.trim();

    if (!empId) {
        nameInput.value = "";
    emailInput.value = "";
    locationSelect.value = "";
    departmentSelect.value = "";
    departmentContainer.style.display = "none";
    idError.textContent = "Please enter an Employee ID";
    return;
                }

    try {
                    const response = await fetch(`/Admin/GetEmployee?id=${encodeURIComponent(empId)}`);
    const data = await response.json();

    if (data.status === "Success") {
        nameInput.value = data.employee;
    emailInput.value = data.email;
    idError.textContent = "";
                    } else {
        nameInput.value = "";
    emailInput.value = "";
    idError.textContent = "Employee not found";
    locationSelect.value = "";
    departmentSelect.value = "";
    departmentContainer.style.display = "none";
                    }
                } catch (error) {
        console.error("Error fetching employee:", error);
    nameInput.value = "";
    emailInput.value = "";
    idError.textContent = "Error fetching employee";
                }
            });

    locationSelect.addEventListener("change", async function () {
                const selectedLocation = this.value;
    departmentSelect.innerHTML = '<option value="">Loading...</option>';

    if (!selectedLocation) {
        departmentContainer.style.display = "none";
    departmentSelect.innerHTML = '<option value="">Select Location First</option>';
    return;
                }

    try {
                    const response = await fetch(`/Admin/GetDepartmentsByLocation?location=${encodeURIComponent(selectedLocation)}`);
    const departments = await response.json();

    departmentSelect.innerHTML = '<option value="">Select Department</option>';

                    if (departments.length > 0) {
        departments.forEach(dept => {
            const option = document.createElement("option");
            option.value = dept.name;
            option.textContent = `${dept.name} (${dept.location})`;
            option.dataset.location = dept.location;
            departmentSelect.appendChild(option);
        });
    departmentContainer.style.display = "block";
                    } else {
        departmentSelect.innerHTML = '<option value="">No departments found</option>';
    departmentContainer.style.display = "block";
                    }
                } catch (error) {
        console.error("Error loading departments:", error);
    departmentSelect.innerHTML = '<option value="">Error loading departments</option>';
                }
            });

    departmentSelect.addEventListener("change", async function() {
                const department = this.value;
    const location = locationSelect.value;

    if (!department || !location) {
        departmentError.style.display = "none";
    departmentError.textContent = "";
    return;
                }

    try {
                    const response = await fetch(`/Admin/IsDepartmentAssigned?departmentName=${encodeURIComponent(department)}&location=${encodeURIComponent(location)}`);
    const data = await response.json();

    if (data.isAssigned) {
        departmentError.textContent = "This department is already assigned to another supervisor";
    departmentError.style.display = "block";
    showMessage('error', 'This department is already assigned to another supervisor');
                        setTimeout(() => {
        departmentError.style.display = "none";
    departmentError.textContent = "";
                        }, 3000);
                    } else {
        departmentError.style.display = "none";
    departmentError.textContent = "";
                    }
                } catch (error) {
        console.error("Error checking department assignment:", error);
    departmentError.textContent = "Error checking department availability";
    departmentError.style.display = "block";
    showMessage('error', 'Error checking department availability');
                    setTimeout(() => {
        departmentError.style.display = "none";
    departmentError.textContent = "";
                    }, 3000);
                }
            });

    form.addEventListener("submit", async function(e) {
        e.preventDefault();

    document.getElementById('loadingIndicator').style.display = 'block';
    document.getElementById('submitBtn').disabled = true;

    const empId = empIdInput.value.trim();
    const name = nameInput.value.trim();
    const email = emailInput.value.trim();
    const location = locationSelect.value;
    const department = departmentSelect.value;

    idError.textContent = "";
    departmentError.style.display = "none";
    departmentError.textContent = "";

    if (!empId) {
        idError.textContent = "Employee ID is required";
    showMessage('error', 'Employee ID is required');
    document.getElementById('loadingIndicator').style.display = 'none';
    document.getElementById('submitBtn').disabled = false;
    return;
            }

    if (!name) {
        idError.textContent = "Employee name is required";
    showMessage('error', 'Employee name is required');
    document.getElementById('loadingIndicator').style.display = 'none';
    document.getElementById('submitBtn').disabled = false;
    return;
            }

    if (!email) {
        idError.textContent = "Employee email is required";
    showMessage('error', 'Employee email is required');
    document.getElementById('loadingIndicator').style.display = 'none';
    document.getElementById('submitBtn').disabled = false;
    return;
            }

    if (!location) {
        showMessage('error', 'Please select a location');
    document.getElementById('loadingIndicator').style.display = 'none';
    document.getElementById('submitBtn').disabled = false;
    return;
            }

    // Create form data
    const formData = new FormData();
    formData.append('Id', empId);
    formData.append('Name', name);
    formData.append('Email', email);
    formData.append('Location', location);
    if (department) {
        formData.append('DepartmentName', department);
            }

    try {
                const response = await fetch('/Admin/AddSupervisor', {
        method: 'POST',
    body: formData,
    headers: {
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                });

    if (response.redirected) {
        window.location.href = response.url;
                } else {
                    const result = await response.json();
    if (result.success) {
        showMessage('success', result.message);
                        setTimeout(() => {
        window.location.href = '@Url.Action("ViewSupervisorInfo", "Admin")';
                        }, 1500);
                    } else {
        showMessage('error', result.message);
                    }
                }
            } catch (error) {
        console.error('Error:', error);
    showMessage('error', 'An error occurred while adding the supervisor');
            } finally {
        document.getElementById('loadingIndicator').style.display = 'none';
    document.getElementById('submitBtn').disabled = false;
            }
        });

    document.getElementById("dark-mode-toggle")?.addEventListener("click", function () {
        document.body.classList.toggle("dark-mode");
    const icon = this.querySelector("i");
    if (icon) {
        icon.classList.toggle("fa-moon");
    icon.classList.toggle("fa-sun");
                }
            });

    document.getElementById("cabinet-management-toggle")?.addEventListener("click", function () {
                const submenu = document.getElementById("cabinet-submenu");
    if (submenu) {
        submenu.classList.toggle("show");
                }
            });
        });
