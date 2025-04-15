// wwwroot/js/admin-reports.js
$(document).ready(function () {
    // Initialize Bootstrap modals
    const detailsModal = new bootstrap.Modal(document.getElementById('detailsModal'));
    const resolveModal = new bootstrap.Modal(document.getElementById('resolveModal'));
    const deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));

    let showSolved = false;

    // Dark mode toggle
    $('#dark-mode-toggle').click(function () {
        $('body').toggleClass('dark-mode');
        $(this).find('i').toggleClass('fa-moon fa-sun');
        localStorage.setItem('darkMode', $('body').hasClass('dark-mode'));
    });

    // Initialize dark mode from localStorage
    if (localStorage.getItem('darkMode') === 'true') {
        $('body').addClass('dark-mode');
        $('#dark-mode-toggle i').removeClass('fa-moon').addClass('fa-sun');
    }

    // Cabinet management submenu toggle
    $('#cabinet-management-toggle').click(function () {
        $('#cabinet-submenu').toggleClass('show');
    });

    // Hide solved reports by default
    $('.solved-row').hide();

    // Toggle solved reports
    $('#toggleSolved').click(function () {
        showSolved = !showSolved;
        filterReports();
        $(this).html(showSolved
            ? '<i class="fas fa-eye-slash"></i> Hide Solved'
            : '<i class="fas fa-eye"></i> Show Solved'
        );
        $(this).toggleClass('btn-outline-secondary', !showSolved);
        $(this).toggleClass('btn-outline-primary', showSolved);
    });

    // Status filter
    $('#status-filter').change(function () {
        filterReports();
    });

    function filterReports() {
        const statusFilter = $('#status-filter').val();

        $('.clickable-row').each(function () {
            const rowStatus = $(this).data('status');
            const isResolved = rowStatus === 'resolved';
            const statusMatch = statusFilter === 'all' ||
                (statusFilter === 'pending' && !isResolved) ||
                (statusFilter === 'resolved' && isResolved);

            $(this).toggle(statusMatch && (!isResolved || showSolved));
        });
    }

    // Show report details
    $('#report-list').on('click', '.clickable-row', function (e) {
        if ($(e.target).is('button, i')) return;

        const reportId = $(this).data('report-id');
        $.ajax({
            url: `/Admin/GetReportDetails/${reportId}`,
            method: 'GET',
            success: function (report) {
                $('#detailsModalTitle').text(`Report Details - Locker ${report.locker.lockerId}`);
                $('#detailsModalBody').html(`
                    <p><strong>Locker Number:</strong> ${report.locker.lockerId}</p>
                    <p><strong>Department:</strong> ${report.locker.department}</p>
                    <p><strong>Reported By:</strong> ${report.reporter.name} (${report.reporter.email})</p>
                    <p><strong>Full Description:</strong> ${report.statement.replace(/\n/g, '<br/>')}</p>
                    ${report.status === 'RESOLVED' ? `
                        <div class="resolution-panel">
                            <div class="resolution-header">
                                <i class="fas fa-check-circle"></i> Resolution Details
                            </div>
                            <p><strong>Resolved on:</strong> ${report.resolvedDate ?
                            new Date(report.resolvedDate).toLocaleString('en-US', {
                                month: 'short',
                                day: '2-digit',
                                year: 'numeric',
                                hour: '2-digit',
                                minute: '2-digit',
                                hour12: true
                            }) : 'N/A'}</p>
                            <div class="border-top pt-2 mt-2">
                                ${report.resolutionDetails ?
                            report.resolutionDetails.replace(/\n/g, '<br/>') :
                            '<div class="alert alert-info"><i class="fas fa-info-circle"></i> No resolution details provided.</div>'
                        }
                            </div>
                        </div>
                    ` : ''}
                `);
                detailsModal.show();
            },
            error: function (xhr) {
                showErrorMessage('Failed to load report details: ' + (xhr.responseText || 'Unknown error'));
            }
        });
    });

    // Resolve report
    $('#report-list').on('click', '.resolve-btn', function (e) {
        e.stopPropagation();
        const reportId = $(this).data('report-id');
        const lockerId = $(this).data('locker-id');
        $('#resolveModalTitle').text(`Resolve Report #${reportId}`);
        $('#resolveReportId').val(reportId);
        $('#resolutionDetails').val('');
        resolveModal.show();
    });

    // Delete report
    $('#report-list').on('click', '.delete-btn', function (e) {
        e.stopPropagation();
        const reportId = $(this).data('report-id');
        const lockerId = $(this).data('locker-id');
        const subject = $(this).data('subject');
        $('#deleteLockerId').text(lockerId);
        $('#deleteSubject').text(subject);
        $('#deleteReportId').val(reportId);
        deleteModal.show();
    });

    // Show error message
    function showErrorMessage(message) {
        const $errorBox = $('<div>', {
            class: 'alert alert-danger',
            role: 'alert',
            text: message
        });
        $('.management').prepend($errorBox);
        setTimeout(() => $errorBox.fadeOut(() => $errorBox.remove()), 5000);
    }
});