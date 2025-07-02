

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

function getFirstMondayOfYear(year) {
    const jan1 = new Date(year, 0, 1); // tháng 0 = tháng 1
    const day = jan1.getDay(); // 0=CN, 1=T2,...,6=T7

    // Tính số ngày để tiến tới thứ Hai
    const diff = (day === 0) ? 1 : (8 - day);
    const firstMonday = new Date(year, 0, 1 + diff - 1);

    // Trả về chuỗi yyyy-MM-dd
    return firstMonday.toISOString().split('T')[0];
}

function openSlotModal(el){
    const scheduleId = el.dataset.id;
    const day = el.dataset.day;
    const start = el.dataset.start;
    const end = el.dataset.end;
    const roomName = el.dataset.room;
    const status = el.dataset.status;
    const isToday = el.dataset.istoday == "True";
    const slot = el.dataset.slot;

    // console.log(isToday);

	document.getElementById("modalDate").textContent = day;
	document.getElementById("modalTime").textContent = `${start} - ${end}`;
	document.getElementById("modalRoom").textContent = roomName;
    document.getElementById("modalSlot").textContent = slot;

	document.getElementById("modalStatus").value = status;
	document.getElementById("modalScheduleId").value = scheduleId;

    document.getElementById("modalStatus").disabled = !isToday;
	document.getElementById("saveStatusBtn").disabled = !isToday;

    const modal = new bootstrap.Modal(document.getElementById('slotModal'));
    modal.show();
}

function updateScheduleStatus(){
   
    const status = document.getElementById('modalStatus').value;
    const scheduleId = document.getElementById('modalScheduleId').value;

    // alert(" status: " + status + " scheduleid: "+scheduleId);
    // return; //debug

    if (!scheduleId) {
        alert("Không xác định được lịch cần thay đổi.");
        return;
    }

    if (!confirm("Bạn xác nhận thay đổi trạng thái này?")) return;

    $.ajax({
        url: "/Schedule/UpdateScheduleStatus",
        type: "POST",
        data: { scheduleId: scheduleId, status: status },
        success: function (res) {
            if (res.success) {
                $('#slotModal').modal('hide');
                alert("Đã thay đổi thành công.");
                updateSchedule(); // cập nhật lại lịch
            } else {
                alert("Thay đổi thất bại: " + res.message);
            }
        },
        error: function () {
            alert("Có lỗi xảy ra khi thay đổi.");
        }
    });
}