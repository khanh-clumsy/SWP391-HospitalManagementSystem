

function updateSchedule(newyear = 0) {
    let year = newyear !== 0 ? newyear : document.getElementById("yearDropdown").value;
    let weekStart = newyear !== 0
    ? getFirstMondayOfYear(newyear)
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

function getFirstMondayOfYear(year) {
    const jan1 = new Date(year, 0, 1); // tháng 0 = tháng 1
    const day = jan1.getDay(); // 0=CN, 1=T2,...,6=T7

    // Tính số ngày để tiến tới thứ Hai
    const diff = (day === 0) ? 1 : (8 - day);
    const firstMonday = new Date(year, 0, 1 + diff - 1);

    // Trả về chuỗi yyyy-MM-dd
    return firstMonday.toISOString().split('T')[0];
}
