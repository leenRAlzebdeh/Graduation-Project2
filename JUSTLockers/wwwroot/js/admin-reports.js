// Initialize when DOM is ready
$(document).ready(function () {
    // Dark mode toggle
    $("#dark-mode-toggle").click(function () {
        $("body").toggleClass("dark-mode");
        $(this).find("i").toggleClass("fa-moon fa-sun");
        localStorage.setItem("darkMode", $("body").hasClass("dark-mode"));
    });

    // Initialize dark mode from localStorage
    if (localStorage.getItem("darkMode") === "true") {
        $("body").addClass("dark-mode");
        $("#dark-mode-toggle i").removeClass("fa-moon").addClass("fa-sun");
    }

    // Cabinet management toggle
    $("#cabinet-management-toggle").click(function () {
        $("#cabinet-submenu").toggleClass("show");
    });

    // Toggle report details
    $(".report-row").on("click", function (e) {
        if (!$(e.target).is("button, .action-btn, i")) {
            const $detailsRow = $(this).next(".report-details-row");
            $detailsRow.toggle();
            $(this).find(".expand-icon").toggleClass("fa-chevron-right fa-chevron-down");
        }
    });

    // Filter reports
    $("#status-filter, #type-filter").change(function () {
        filterReports();
    });

    function filterReports() {
        const statusFilter = $("#status-filter").val();
        const typeFilter = $("#type-filter").val();

        $(".report-row").each(function () {
            const rowStatus = $(this).data("status");
            const rowType = $(this).data("type");

            const statusMatch = statusFilter === "all" || rowStatus === statusFilter;
            const typeMatch = typeFilter === "all" || rowType === typeFilter;

            $(this).toggle(statusMatch && typeMatch);

            // Hide details if parent row is hidden
            if (!$(this).is(":visible")) {
                $(this).next(".report-details-row").hide();
                $(this).find(".expand-icon").removeClass("fa-chevron-down").addClass("fa-chevron-right");
            }
        });
    }

    // Solve report
    $(".solve-btn").on("click", function (e) {
        e.stopPropagation();
        const reportId = $(this).data("id");
        $("#resolveReportId").val(reportId);
        $("#resolutionDetails").val("");
        $("#resolveModal").fadeIn().attr("aria-hidden", "false");
    });

    // Delete report
    $(".delete-btn").on("click", function (e) {
        e.stopPropagation();
        const reportId = $(this).data("id");
        const $row = $(this).closest(".report-row");
        const lockerId = $row.find("td").eq(0).text().trim();
        const subject = $row.find("td").eq(1).text().trim();

        $("#deleteReportId").val(reportId);
        $("#deleteLockerId").text(lockerId);
        $("#deleteSubject").text(subject);
        $("#deleteModal").fadeIn().attr("aria-hidden", "false");
    });

    // Close modal handlers
    $(".close-modal").on("click", function () {
        closeModal($(this).closest(".modal-overlay"));
    });

    // Close modal when clicking outside
    $(".modal-overlay").on("click", function (e) {
        if ($(e.target).hasClass("modal-overlay")) {
            closeModal($(this));
        }
    });

    function closeModal($modal) {
        $modal.fadeOut().attr("aria-hidden", "true");
        $modal.find("textarea, input[type=hidden]").val("");
    }

    // Submit resolve form
    $("#resolveForm").on("submit", function (e) {
        e.preventDefault();
        const reportId = $("#resolveReportId").val();
        const resolutionDetails = $("#resolutionDetails").val();

        $.ajax({
            url: "/Admin/ResolveReport",
            type: "POST",
            data: {
                reportId: reportId,
                resolutionDetails: resolutionDetails,
                __RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
            },
            success: function () {
                location.reload();
            },
            error: function (xhr) {
                showErrorMessage("Failed to resolve report: " + (xhr.responseText || "Unknown error"));
            },
            complete: function () {
                closeModal($("#resolveModal"));
            }
        });
    });

    // Submit delete form
    $("#deleteForm").on("submit", function (e) {
        e.preventDefault();
        const reportId = $("#deleteReportId").val();

        $.ajax({
            url: "/Admin/DeleteReport",
            type: "POST",
            data: {
                reportId: reportId,
                __RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
            },
            success: function () {
                location.reload();
            },
            error: function (xhr) {
                showErrorMessage("Failed to delete report: " + (xhr.responseText || "Unknown error"));
            },
            complete: function () {
                closeModal($("#deleteModal"));
            }
        });
    });

    // Show error message
    function showErrorMessage(message) {
        const $errorBox = $("<div>", {
            class: "message-box error",
            role: "alert",
            text: message
        });
        $(".reports-section").prepend($errorBox);
        setTimeout(() => $errorBox.fadeOut(() => $errorBox.remove()), 5000);
    }

    // Export reports (placeholder for actual implementation)
    $("#export-reports").click(function () {
        $.ajax({
            url: "/Admin/ExportReports",
            type: "GET",
            success: function (data) {
                const blob = new Blob([data], { type: "text/csv" });
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = "reports.csv";
                a.click();
                window.URL.revokeObjectURL(url);
            },
            error: function (xhr) {
                showErrorMessage("Failed to export reports: " + (xhr.responseText || "Unknown error"));
            }
        });
    });
});