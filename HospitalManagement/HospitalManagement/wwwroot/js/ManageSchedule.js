

function updateSchedule(newyear = 0) {
    let year = newyear !== 0 ? newyear : document.getElementById("yearDropdown").value;
    let weekStart = newyear !== 0
    ? (new Date(newyear, 0, 1)).toLocaleDateString().split('T')[0]
    : document.getElementById("weekDropdown").value;
    
    $.ajax({
        url: '/Schedule/GetScheduleTable2',
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

function toggleSlot(el) {
    const hiddenInput = el.parentElement.querySelector("input[type=hidden]");
    const current = hiddenInput.value === "true";

    if (current) {
        // Chuyển về false
        hiddenInput.value = "false";
        el.classList.remove("slot-active");
        el.classList.add("slot-inactive");
        el.querySelector("div:last-child").innerHTML = '<i class="fas fa-bars"></i> Chưa chọn';
    } else {
        // Chuyển sang true
        hiddenInput.value = "true";
        el.classList.remove("slot-inactive");
        el.classList.add("slot-active");
        el.querySelector("div:last-child").innerHTML = '<i class="fas fa-check-circle"></i> Đã chọn';
    }
}
