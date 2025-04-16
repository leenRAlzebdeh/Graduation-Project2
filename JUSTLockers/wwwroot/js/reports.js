// reports.js
document.addEventListener("DOMContentLoaded", function () {
    // Initialize Bootstrap modals
    const resolveModal = new bootstrap.Modal(document.getElementById('resolveModal'));
    const rejectModal = new bootstrap.Modal(document.getElementById('rejectModal'));

    let showSolved = false;

    // Initialize toggle button
    const toggleSolvedButton = document.getElementById('toggleSolved');
    if (toggleSolvedButton) {
        toggleSolvedButton.addEventListener('click', function () {
            showSolved = !showSolved;
            filterReports();
            this.innerHTML = showSolved
                ? '<i class="fas fa-eye-slash"></i> Hide Resolved/Rejected'
                : '<i class="fas fa-eye"></i> Show Resolved/Rejected';
            this.classList.toggle('btn-outline-secondary', !showSolved);
            this.classList.toggle('btn-outline-primary', showSolved);
        });
    }

    // Status filter
    const statusFilter = document.getElementById('status-filter');
    if (statusFilter) {
        statusFilter.addEventListener('change', filterReports);
    }

    function filterReports() {
        const statusFilterValue = statusFilter ? statusFilter.value : 'all';

        document.querySelectorAll('.clickable-row').forEach(row => {
            const rowStatus = row.dataset.status;
            const isFinal = rowStatus === 'resolved' || rowStatus === 'rejected';
            const statusMatch = statusFilterValue === 'all' || statusFilterValue === rowStatus;

            const shouldShow = statusMatch && (!isFinal || showSolved);
            row.style.display = shouldShow ? '' : 'none';

            // Hide corresponding details row
            const detailsRow = document.querySelector(`.report-details-row[data-report-id="${row.dataset.reportId}"]`);
            if (detailsRow) detailsRow.style.display = 'none';
        });
    }

    // Toggle report details
    const reportList = document.getElementById('report-list');
    if (reportList) {
        reportList.addEventListener('click', function (e) {
            if (e.target.closest('.clickable-row') && !e.target.closest('.action-btn, i')) {
                const row = e.target.closest('.clickable-row');
                const reportId = row.dataset.reportId;
                const detailsRow = document.querySelector(`.report-details-row[data-report-id="${reportId}"]`);

                if (detailsRow) {
                    if (detailsRow.style.display === 'none' || detailsRow.style.display === '') {
                        // Hide all other details first
                        document.querySelectorAll('.report-details-row').forEach(detail => {
                            detail.style.display = 'none';
                        });
                        // Show the clicked row's details
                        detailsRow.style.display = '';
                    } else {
                        detailsRow.style.display = 'none';
                    }
                }
            }
        });
    }

    // Resolve report button handler
    if (reportList) {
        reportList.addEventListener('click', function (e) {
            const button = e.target.closest('.resolve-btn');
            if (button) {
                e.stopPropagation();
                const reportId = button.dataset.reportId;
                document.getElementById('resolveModalTitle').textContent = `Resolve Report #${reportId}`;
                document.getElementById('resolveReportId').value = reportId;
                document.getElementById('resolvedDetails').value = '';
                resolveModal.show();
            }
        });
    }

    // Reject report button handler
    if (reportList) {
        reportList.addEventListener('click', function (e) {
            const button = e.target.closest('.reject-btn');
            if (button) {
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
    }

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
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        resolveModal.hide();
                        const reportId = formData.get('reportId');
                        const row = document.querySelector(`.clickable-row[data-report-id="${reportId}"]`);
                        if (row) {
                            row.dataset.status = 'resolved';
                            row.classList.remove('unsolved-row');
                            row.classList.add('solved-row');
                            const statusBadge = row.querySelector('.status-badge');
                            if (statusBadge) {
                                statusBadge.textContent = 'RESOLVED';
                                statusBadge.className = 'status-badge resolved';
                            }
                            const actionsCell = row.querySelector('td:last-child');
                            if (actionsCell) actionsCell.innerHTML = '';
                        }
                        filterReports();
                    } else {
                        showErrorMessage(data.message || 'Failed to resolve report');
                    }
                })
                .catch(error => {
                    console.error('Error resolving report:', error);
                    showErrorMessage('Error resolving report');
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
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        rejectModal.hide();
                        const reportId = formData.get('reportId');
                        const row = document.querySelector(`.clickable-row[data-report-id="${reportId}"]`);
                        if (row) {
                            row.dataset.status = 'rejected';
                            row.classList.remove('unsolved-row');
                            row.classList.add('solved-row');
                            const statusBadge = row.querySelector('.status-badge');
                            if (statusBadge) {
                                statusBadge.textContent = 'REJECTED';
                                statusBadge.className = 'status-badge rejected';
                            }
                            const actionsCell = row.querySelector('td:last-child');
                            if (actionsCell) {
                                actionsCell.innerHTML = `
                                <button class="action-btn solve-btn resolve-btn"
                                        data-report-id="${reportId}"
                                        data-locker-id="${button?.dataset.lockerId || ''}">
                                    <i class="fas fa-check"></i> Solve
                                </button>
                            `;
                            }
                        }
                        filterReports();
                    } else {
                        showErrorMessage(data.message || 'Failed to reject report');
                    }
                })
                .catch(error => {
                    console.error('Error rejecting report:', error);
                    showErrorMessage('Error rejecting report');
                });
        });
    }

    // Show error message
    function showErrorMessage(message) {
        const errorBox = document.createElement('div');
        errorBox.className = 'message-box error';
        errorBox.textContent = message;
        const formSection = document.querySelector('.form-section');
        if (formSection) {
            formSection.prepend(errorBox);
            setTimeout(() => {
                errorBox.style.opacity = '0';
                setTimeout(() => errorBox.remove(), 300);
            }, 5000);
        }
    }

    // Apply initial filter on page load
    filterReports();
});