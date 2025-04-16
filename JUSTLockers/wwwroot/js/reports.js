document.addEventListener("DOMContentLoaded", function () {
    // Initialize Bootstrap modals
    const detailsModal = new bootstrap.Modal(document.getElementById('detailsModal'));
    const resolveModal = new bootstrap.Modal(document.getElementById('resolveModal'));
    const rejectModal = new bootstrap.Modal(document.getElementById('rejectModal'));

    let showSolved = false;

    // Initialize toggle button
    const toggleSolvedButton = document.getElementById('toggleSolved');
    if (toggleSolvedButton) {
        toggleSolvedButton.innerHTML = '<i class="fas fa-eye"></i> Show Resolved/Rejected';
        toggleSolvedButton.classList.add('btn-outline-secondary');
        toggleSolvedButton.addEventListener('click', function () {
            showSolved = !showSolved;
            console.log('Toggled showSolved to:', showSolved);
            filterReports();
            this.innerHTML = showSolved
                ? '<i class="fas fa-eye-slash"></i> Hide Resolved/Rejected'
                : '<i class="fas fa-eye"></i> Show Resolved/Rejected';
            this.classList.toggle('btn-outline-secondary', !showSolved);
            this.classList.toggle('btn-outline-primary', showSolved);
        });
    } else {
        console.error('toggleSolved button not found');
    }

    // Status filter
    const statusFilter = document.getElementById('status-filter');
    if (statusFilter) {
        statusFilter.addEventListener('change', function () {
            console.log('Status filter changed to:', this.value);
            filterReports();
        });
    } else {
        console.error('status-filter dropdown not found');
    }

    function filterReports() {
        const statusFilterValue = statusFilter ? statusFilter.value : 'all';
        console.log('Filtering reports with status:', statusFilterValue, 'showSolved:', showSolved);
        document.querySelectorAll('.clickable-row').forEach(row => {
            const rowStatus = row.dataset.status;
            if (!['reported', 'resolved', 'rejected'].includes(rowStatus)) {
                console.warn(`Invalid data-status value: ${rowStatus} for report ID: ${row.dataset.reportId}`);
                row.style.display = 'none';
                return;
            }
            const isFinal = rowStatus === 'resolved' || rowStatus === 'rejected';
            const statusMatch = statusFilterValue === 'all' || statusFilterValue === rowStatus;
            const shouldShow = statusMatch && (!isFinal || showSolved);
            row.style.display = shouldShow ? 'table-row' : 'none';
        });
    }

    // Show report details in modal
    document.addEventListener('click', function (e) {
        const row = e.target.closest('.clickable-row');
        if (!row || e.target.closest('.action-btn')) return;

        const reportId = row.dataset.reportId;
        document.getElementById('detailsModalTitle').textContent = `Report Details - #${reportId}`;
        document.getElementById('detailsLockerId').textContent = row.dataset.lockerId || 'N/A';
        document.getElementById('detailsDepartment').textContent = row.dataset.department || 'N/A';
        document.getElementById('detailsReporterName').textContent = row.dataset.reporterName || 'N/A';
        document.getElementById('detailsReporterEmail').textContent = row.dataset.reporterEmail || 'N/A';
        document.getElementById('detailsReporterDepartment').textContent = row.dataset.reporterDepartment || 'N/A';
        document.getElementById('detailsReportType').textContent = row.dataset.reportType || 'N/A';
        document.getElementById('detailsStatement').textContent = row.dataset.statement || 'No description provided';

        const resolutionPanel = document.getElementById('detailsResolutionPanel');
        if (row.dataset.status === 'resolved' || row.dataset.status === 'rejected') {
            resolutionPanel.style.display = 'block';
            document.getElementById('detailsResolvedDate').textContent = row.dataset.resolvedDate || 'N/A';
            const resolutionDetails = row.dataset.resolutionDetails;
            document.getElementById('detailsResolutionDetails').innerHTML = resolutionDetails
                ? resolutionDetails.replace(/\n/g, '<br>')
                : '<div class="alert alert-info"><i class="fas fa-info-circle"></i> No resolution details provided.</div>';
        } else {
            resolutionPanel.style.display = 'none';
        }

        detailsModal.show();
    });

    // Resolve report button handler
    document.addEventListener('click', function (e) {
        const button = e.target.closest('.resolve-btn');
        if (button) {
            e.preventDefault();
            e.stopPropagation();
            const reportId = button.dataset.reportId;
            document.getElementById('resolveModalTitle').textContent = `Resolve Report #${reportId}`;
            document.getElementById('resolveReportId').value = reportId;
            document.getElementById('resolvedDetails').value = '';
            resolveModal.show();
        }
    });

    // Reject report button handler
    document.addEventListener('click', function (e) {
        const button = e.target.closest('.reject-btn');
        if (button) {
            e.preventDefault();
            e.stopPropagation();
            const reportId = button.dataset.reportId;
            const lockerId = button.dataset.lockerId;
            const subject = button.dataset.subject;

            document.getElementById('rejectModalTitle').textContent = `Reject Report - Locker ${lockerId}`;
            document.getElementById('rejectLockerId').textContent = lockerId;
            document.getElementById('rejectSubject').textContent = subject;
            document.getElementById('rejectReportId').value = reportId;

            rejectModal.show();
        }
    });

    // Resolve form submission
    const resolveForm = document.getElementById('resolveForm');
    if (resolveForm) {
        resolveForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = new FormData(this);
            fetch(this.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            })
                .then(response => {
                    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                    return response.json();
                })
                .then(data => {
                    if (data.success) {
                        resolveModal.hide();
                        const reportId = formData.get('reportId');
                        updateReportStatus(reportId, 'resolved', formData.get('resolutionDetails'));
                        filterReports();
                        showSuccessMessage(data.message);
                    } else {
                        showErrorMessage(data.message);
                    }
                })
                .catch(error => {
                    console.error('Error resolving report:', error);
                    showErrorMessage(`Failed to resolve report: ${error.message}`);
                });
        });
    }

    // Reject form submission
    const rejectForm = document.getElementById('rejectForm');
    if (rejectForm) {
        rejectForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = new FormData(this);
            fetch(this.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            })
                .then(response => {
                    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                    return response.json();
                })
                .then(data => {
                    if (data.success) {
                        rejectModal.hide();
                        const reportId = formData.get('reportId');
                        updateReportStatus(reportId, 'rejected');
                        filterReports();
                        showSuccessMessage(data.message);
                    } else {
                        showErrorMessage(data.message);
                    }
                })
                .catch(error => {
                    console.error('Error rejecting report:', error);
                    showErrorMessage(`Failed to reject report: ${error.message}`);
                });
        });
    }

    // Helper function to update report status in UI
    function updateReportStatus(reportId, newStatus, resolutionDetails = '') {
        const row = document.querySelector(`.clickable-row[data-report-id="${reportId}"]`);
        if (!row) return;

        row.dataset.status = newStatus;
        row.classList.remove('unsolved-row');
        row.classList.add('solved-row');

        const statusBadge = row.querySelector('.status-badge');
        if (statusBadge) {
            statusBadge.textContent = newStatus.toUpperCase();
            statusBadge.className = `status-badge ${newStatus}`;
        }

        const actionsCell = row.querySelector('td:last-child');
        if (actionsCell) {
            if (newStatus === 'rejected') {
                actionsCell.innerHTML = `
                    <button class="action-btn solve-btn resolve-btn"
                            data-report-id="${reportId}"
                            data-locker-id="${row.dataset.lockerId}">
                        <i class="fas fa-check"></i> Solve
                    </button>
                `;
            } else {
                actionsCell.innerHTML = '';
            }
        }

        // Update modal data attributes
        row.dataset.resolvedDate = newStatus !== 'reported' ? new Date().toLocaleString('en-US', {
            month: 'short',
            day: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        }) : 'N/A';
        row.dataset.resolutionDetails = resolutionDetails || row.dataset.resolutionDetails || '';
    }

    // Show success message
    function showSuccessMessage(message) {
        const successBox = document.createElement('div');
        successBox.className = 'message-box success';
        successBox.textContent = message;
        showMessage(successBox);
    }

    // Show error message
    function showErrorMessage(message) {
        const errorBox = document.createElement('div');
        errorBox.className = 'message-box error';
        errorBox.textContent = message;
        showMessage(errorBox);
    }

    // Generic message display
    function showMessage(messageElement) {
        const formSection = document.querySelector('.form-section');
        if (formSection) {
            document.querySelectorAll('.message-box').forEach(msg => msg.remove());
            formSection.prepend(messageElement);
            setTimeout(() => {
                messageElement.style.opacity = '0';
                setTimeout(() => messageElement.remove(), 300);
            }, 5000);
        }
    }

    // Apply initial filter on page load
    filterReports();
});