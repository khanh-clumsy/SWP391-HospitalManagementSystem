

function updateSchedule(newyear = 0) {
    let year = newyear !== 0 ? newyear : document.getElementById("yearDropdown").value;
    let weekStart = newyear !== 0
    ? (new Date(newyear, 0, 1)).toLocaleDateString().split('T')[0]
    : document.getElementById("weekDropdown").value;
    
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
