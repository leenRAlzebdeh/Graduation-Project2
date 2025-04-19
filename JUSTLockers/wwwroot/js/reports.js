document.addEventListener("DOMContentLoaded", function () {
    // Initialize all modals
    const detailsModal = new bootstrap.Modal(document.getElementById('detailsModal'));
    const resolveModal = new bootstrap.Modal(document.getElementById('resolveModal'));
    const rejectModal = new bootstrap.Modal(document.getElementById('rejectModal'));
    const reviewModal = new bootstrap.Modal(document.getElementById('reviewModal'));

    let showSolved = false;

    // Toggle solved reports button
    const toggleSolvedButton = document.getElementById('toggleSolved');
    if (toggleSolvedButton) {
        toggleSolvedButton.addEventListener('click', function () {
            showSolved = !showSolved;
            this.innerHTML = showSolved
                ? '<i class="fas fa-eye-slash"></i> Hide Resolved/Rejected'
                : '<i class="fas fa-eye"></i> Show Resolved/Rejected';
            filterReports();
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
            const rowStatus = row.dataset.status.toLowerCase();
            const isFinal = rowStatus === 'resolved' || rowStatus === 'rejected';
            const statusMatch = statusFilterValue === 'all' ||
                statusFilterValue.toLowerCase() === rowStatus;
            const shouldShow = statusMatch && (!isFinal || showSolved);
            row.style.display = shouldShow ? '' : 'none';
        });
    }

    // Report details click handler
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


    // Resolve button handler
    document.addEventListener('click', function (e) {
        const button = e.target.closest('.resolve-btn');
        if (button) {
            e.preventDefault();
            e.stopPropagation();
            document.getElementById('resolveModalTitle').textContent = `Resolve Report #${button.dataset.reportId}`;
            document.getElementById('resolveReportId').value = button.dataset.reportId;
            document.getElementById('resolvedDetails').value = '';
            resolveModal.show();
        }
    });

    // Reject button handler
    document.addEventListener('click', function (e) {
        const button = e.target.closest('.reject-btn');
        if (button) {
            e.preventDefault();
            e.stopPropagation();
            document.getElementById('rejectModalTitle').textContent = `Reject Report - Locker ${button.dataset.lockerId}`;
            document.getElementById('rejectLockerId').textContent = button.dataset.lockerId;
            document.getElementById('rejectSubject').textContent = button.dataset.subject;
            document.getElementById('rejectReportId').value = button.dataset.reportId;
            rejectModal.show();
        }
    });

    // REVIEW BUTTON FIX - This is the critical part
    document.addEventListener('click', function (e) {
        const button = e.target.closest('.review-btn');
        if (button) {
            e.preventDefault();
            e.stopPropagation();
            document.getElementById('reviewReportId').value = button.dataset.reportId;
            reviewModal.show();
        }
    });

    // Form submission handler
    function setupFormHandler(formId, modal, successCallback) {
        const form = document.getElementById(formId);
        if (form) {
            form.addEventListener('submit', function (e) {
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
                        if (!response.ok) throw new Error('Network error');
                        return response.json();
                    })
                    .then(data => {
                        if (data.success) {
                            modal.hide();
                            successCallback(formData.get('reportId'), data.message);
                            showMessage(data.message, 'success');
                        } else {
                            showMessage(data.message || 'Operation failed', 'error');
                        }
                    })
                    .catch(error => {
                        console.error('Error:', error);
                        showMessage('An error occurred', 'error');
                    });
            });
        }
    }

    // Setup form handlers
    setupFormHandler('resolveForm', resolveModal, (reportId) => {
        updateReportStatus(reportId, 'resolved');
    });

    setupFormHandler('rejectForm', rejectModal, (reportId) => {
        updateReportStatus(reportId, 'rejected');
    });

    setupFormHandler('reviewForm', reviewModal, (reportId) => {
        updateReportStatus(reportId, 'in_review');
    });

    // Update report status in UI
    function updateReportStatus(reportId, newStatus, resolutionDetails = '') {
        const row = document.querySelector(`tr[data-report-id="${reportId}"]`);
        if (!row) return;

        row.dataset.status = newStatus;
        row.classList.remove('unsolved-row');
        row.classList.add('solved-row');

        const statusBadge = row.querySelector('.status-badge');
        if (statusBadge) {
            statusBadge.textContent = newStatus === 'in_review' ? 'IN REVIEW' : newStatus.toUpperCase();
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
            } else if (newStatus === 'in_review') {
                actionsCell.innerHTML = `
                    <button class="action-btn solve-btn resolve-btn"
                            data-report-id="${reportId}"
                            data-locker-id="${row.dataset.lockerId}">
                        <i class="fas fa-check"></i> Solve
                    </button>
                    <button class="action-btn reject-btn"
                            data-report-id="${reportId}"
                            data-locker-id="${row.dataset.lockerId}"
                            data-subject="${row.dataset.subject}">
                        <i class="fas fa-times"></i> Reject
                    </button>
                `;
            } else {
                actionsCell.innerHTML = '';
            }
        }

        if (newStatus !== 'reported') {
            row.dataset.resolvedDate = new Date().toLocaleString('en-US', {
                month: 'short',
                day: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        }

        if (resolutionDetails) {
            row.dataset.resolutionDetails = resolutionDetails;
        }
    }


    // Show message function
    function showMessage(message, type) {
        const messageBox = document.createElement('div');
        messageBox.className = `message-box ${type}`;
        messageBox.textContent = message;

        const formSection = document.querySelector('.form-section');
        if (formSection) {
            document.querySelectorAll('.message-box').forEach(msg => msg.remove());
            formSection.prepend(messageBox);

            setTimeout(() => {
                messageBox.style.opacity = '0';
                setTimeout(() => messageBox.remove(), 300);
            }, 5000);
        }
    }

    // Initial filter
    filterReports();
});