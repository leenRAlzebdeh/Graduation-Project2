// wwwroot/js/reports.js
document.addEventListener("DOMContentLoaded", function () {
    // Dark mode toggle
    document.getElementById("dark-mode-toggle").addEventListener("click", function () {
        document.body.classList.toggle("dark-mode");
        const icon = this.querySelector("i");
        icon.classList.toggle("fa-moon");
        icon.classList.toggle("fa-sun");
        localStorage.setItem('darkMode', document.body.classList.contains('dark-mode'));
    });

    // Initialize dark mode from localStorage
    if (localStorage.getItem('darkMode') === 'true') {
        document.body.classList.add('dark-mode');
        document.getElementById("dark-mode-toggle").querySelector("i").classList.replace('fa-moon', 'fa-sun');
    }

    // Cabinet management submenu toggle
    document.getElementById("cabinet-management-toggle").addEventListener("click", function () {
        const submenu = document.getElementById("cabinet-submenu");
        submenu.classList.toggle("show");
    });

    // Initialize Bootstrap modals
    const resolveModal = new bootstrap.Modal(document.getElementById('resolveModal'));
    const rejectModal = new bootstrap.Modal(document.getElementById('rejectModal'));

    let showSolved = false;

    // Hide resolved/rejected reports by default
    document.querySelectorAll('.solved-row').forEach(row => {
        row.style.display = 'none';
    });

    // Toggle resolved/rejected reports
    document.getElementById('toggleSolved').addEventListener('click', function () {
        showSolved = !showSolved;
        filterReports();
        this.innerHTML = showSolved
            ? '<i class="fas fa-eye-slash"></i> Hide Resolved/Rejected'
            : '<i class="fas fa-eye"></i> Show Resolved/Rejected';
        this.classList.toggle('btn-outline-secondary', !showSolved);
        this.classList.toggle('btn-outline-primary', showSolved);
    });

    // Status filter
    document.getElementById('status-filter').addEventListener('change', function () {
        filterReports();
    });

    function filterReports() {
        const statusFilter = document.getElementById('status-filter').value;
        document.querySelectorAll('.clickable-row').forEach(row => {
            const rowStatus = row.dataset.status;
            const isFinal = rowStatus === 'resolved' || rowStatus === 'rejected';
            const statusMatch = statusFilter === 'all' || statusFilter === rowStatus;

            // Show if status matches and (not final status or showSolved is true)
            const shouldShow = statusMatch && (!isFinal || showSolved);
            row.style.display = shouldShow ? 'table-row' : 'none';

            // Hide corresponding details row
            const detailsRow = document.querySelector(`.report-details-row[data-report-id="${row.dataset.reportId}"]`);
            if (detailsRow) {
                detailsRow.style.display = 'none';
            }
        });
    }

    // Toggle report details when clicking a row
    document.getElementById('report-list').addEventListener('click', function (e) {
        if (e.target.closest('.clickable-row') && !e.target.closest('.action-btn, i')) {
            const row = e.target.closest('.clickable-row');
            const reportId = row.dataset.reportId;
            const detailsRow = document.querySelector(`.report-details-row[data-report-id="${reportId}"]`);

            // Toggle display of the clicked row's details
            if (detailsRow.style.display === 'none') {
                // Hide all other details first
                document.querySelectorAll('.report-details-row').forEach(detail => {
                    detail.style.display = 'none';
                });
                // Show the clicked row's details
                detailsRow.style.display = 'table-row';
            } else {
                // Hide the details if already shown
                detailsRow.style.display = 'none';
            }
        }
    });

    // Resolve report
    document.getElementById('report-list').addEventListener('click', function (e) {
        const button = e.target.closest('.resolve-btn');
        if (button) {
            e.stopPropagation();
            const reportId = button.dataset.reportId;
            const lockerId = button.dataset.lockerId;
            document.getElementById('resolveModalTitle').textContent = `Resolve Report #${reportId}`;
            document.getElementById('resolveReportId').value = reportId;
            document.getElementById('resolvedDetails').value = '';
            resolveModal.show();
        }
    });

    // Reject report
    document.getElementById('report-list').addEventListener('click', function (e) {
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

    // Show error message
    function showErrorMessage(message) {
        const errorBox = document.createElement('div');
        errorBox.className = 'message-box error';
        errorBox.textContent = message;
        document.querySelector('.form-section').prepend(errorBox);
        setTimeout(() => {
            errorBox.style.opacity = '0';
            setTimeout(() => errorBox.remove(), 300);
        }, 5000);
    }
});