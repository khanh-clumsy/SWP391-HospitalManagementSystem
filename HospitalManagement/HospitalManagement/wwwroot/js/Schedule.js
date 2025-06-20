function updateSchedule(newyear) {
    const year = document.getElementById("yearDropdown").value;
    const weekStart = document.getElementById("weekDropdown").value;

    $.ajax({
        url: '/Schedule/GetScheduleTable',
        type: 'GET',
        data: { year: year, weekStart: weekStart },
        success: function (html) {
            $('#scheduleTable').html(html);
        },
        error: function () {
            alert("Lỗi khi tải lịch làm việc.");
        }
    });
}

function changeWeek(offset) {
    const weekDropdown = document.getElementById("weekDropdown");
    const options = weekDropdown.options;
    const current = weekDropdown.selectedIndex;
    const newIndex = current + offset;

    if (newIndex >= 0 && newIndex < options.length) {
        weekDropdown.selectedIndex = newIndex;
        updateSchedule(0);
    }
}
