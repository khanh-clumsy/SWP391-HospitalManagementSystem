// ========== Biến toàn cục ==========
let selectedDep = null;
let selectedDoctor = null;
let selectedSlots = [];

const depSelect = document.getElementById("depSelect");
const doctorSelect = document.getElementById("doctorSelect");
const card = document.getElementById("card");
const cardContent = document.getElementById("cardContent");
const modalDoctorSelect = document.getElementById("modalDoctorId")
// ========== Sự kiện chọn khoa ==========
function changeDepartment(){
    selectedDep = depSelect.value || null;

    // Reset bác sĩ
    doctorSelect.value = "";
    doctorSelect.innerHTML = '<option value="">Mã bác sĩ</option>';
    selectedDoctor = null;

    if (selectedDep) {
        // them danh sach bac si 
        const filteredDoctors = doctorList.filter(doc => doc.departmentName === selectedDep);
        filteredDoctors.forEach(doc => {
            const option = document.createElement("option");
            option.value = doc.doctorId;
            option.textContent = doc.doctorCode;
            doctorSelect.appendChild(option);
        });


        doctorSelect.disabled = false;
    } else {
        doctorSelect.disabled = true;
    }

    updateSchedule(); // reset bảng
    updateCard();
}

// ========== Sự kiện chọn bác sĩ ==========
function changeDoctor(){
    selectedDoctor = doctorSelect.value
        ? doctorSelect.options[doctorSelect.selectedIndex].text
        : null;
    
    updateSchedule();
    updateCard();
}

// ========== Cập nhật thẻ card ==========
function updateCard() {
    cardContent.innerHTML = "";

    if (selectedDep) cardContent.innerHTML += `<div><i class="fa fa-plus-circle"></i> Khoa: ${selectedDep}</div>`;
    if (selectedDoctor) cardContent.innerHTML += `<div><i class="fa fa-user-md"></i> Bác sĩ: ${selectedDoctor}</div>`;

    card.classList.remove("complete");
    if (selectedDep && selectedDoctor) {
        card.classList.add("complete");
    }
}

// ========== Đổi tuần ==========
function changeWeek(offset) {
    const weekDropdown = document.getElementById("weekDropdown");
    const current = weekDropdown.selectedIndex;
    const newIndex = current + offset;
    if (newIndex >= 0 && newIndex < weekDropdown.options.length) {
        weekDropdown.selectedIndex = newIndex;
        updateSchedule();
    }
}

// ========== Làm mới bảng ==========
function updateSchedule(newyear = 0) {
    const year = newyear !== 0 ? newyear : document.getElementById("yearDropdown").value;
    const weekStart = newyear !== 0
        ? (new Date(newyear, 0, 1)).toLocaleDateString().split('T')[0]
        : document.getElementById("weekDropdown").value;

    $.ajax({
        url: '/Schedule/GetScheduleTable3',
        type: 'GET',
        data: { year, weekStart },
        success: function (html) {
            $('#scheduleTable').html(html);
        },
        error: function () {
            alert("Lỗi khi tải lịch làm việc.");
        }
    });
}


function showDoctorSchedule() {
    if (!selectedDoctor) {
        alert("Vui lòng chọn bác sĩ.");
        return;
    }

    const button = document.getElementById("showScheduleBtn");

    if (!button || button.disabled) return;

    button.disabled = true;

    // Delay 1.01 giây rồi mới gửi
    setTimeout(() => {
        $.ajax({
            url: '/Schedule/GetDoctorScheduleInWeek2',
            method: 'POST',
            data: {
                doctorId: document.getElementById("doctorSelect").value,
                year: document.getElementById("yearDropdown").value,
                weekStart: document.getElementById("weekDropdown").value
            },
            success: function (res) {
                document.getElementById("scheduleTable").innerHTML = res;
                button.disabled = false;
            },
            error: function () {
                button.disabled = false;
                alert("Lỗi khi tải lịch");

            }
        });
    }, 1010); // delay 1.01 giây

}

// ========== Mở Modal sửa slot ==========
function openEditModal(el) {

    const doctorId = el.dataset.doctorid;
    const slotId = el.dataset.slotid;
    const roomId = el.dataset.roomid;
    const day = el.dataset.day;
    const scheduleId = el.dataset.scheduleid;


    $('#modalDoctorId').val(doctorId);
    $('#modalDaySelect').val(day);
    $('#modalSlotId').val(slotId);
    $('#modalRoomId').val(roomId);
    $('#modalScheduleId').val(scheduleId);

    // doctor (chua co list ben html)
    const filteredDoctors = doctorList.filter(doc => doc.departmentName === selectedDep);
    modalDoctorSelect.innerHTML = '';

    filteredDoctors.forEach(doc => {
        const option = document.createElement("option");
        option.value = doc.doctorId.toString();
        option.textContent = doc.doctorCode;
        modalDoctorSelect.appendChild(option);
    });

    if (doctorId) {
        modalDoctorSelect.value = doctorId.toString();
    }

    // slot (da co list ben html)
    const modalSlotSelect = document.getElementById("modalSlotId");
    if (slotId) {
        modalSlotSelect.value = slotId.toString();
    }

    // room (da co list ben html)
    const modalRoomSelect = document.getElementById("modalRoomId");
    if (roomId && modalRoomSelect) {
        modalRoomSelect.value = roomId;
    }

    // day (da co list ben html)
    const modalDaySelect = document.getElementById("modalDaySelect");
    if (day && modalDaySelect) {
        modalDaySelect.value = day;
    }

    const modal = new bootstrap.Modal(document.getElementById('editScheduleModal'));
    modal.show();
}

// xoa
function DeleteSchedule() {

    const scheduleId = document.getElementById('modalScheduleId').value;

    if (!scheduleId) {
        alert("Không xác định được lịch cần xoá.");
        return;
    }

    if (!confirm("Bạn có chắc chắn muốn xoá slot này?")) return;

    // Gửi request đến controller
    $.ajax({
        url: "/Schedule/DeleteSchedule", // đường dẫn controller
        type: "POST",
        data: { scheduleId : scheduleId },
        success: function (res) {
            if (res.success) {
                $('#editScheduleModal').modal('hide');
                alert("Đã xoá thành công.");
                showDoctorSchedule(); // cập nhật lại lịch
            } else {
                alert("Xoá thất bại: " + res.message);
            }
        },
        error: function () {
            alert("Có lỗi xảy ra khi xoá.");
        }
    });
}
// update
function UpdateSchedule() {
    const day = document.getElementById('modalDaySelect').value;
    const doctorId = document.getElementById('modalDoctorId').value;
    const slotId = document.getElementById('modalSlotId').value;
    const roomId = document.getElementById('modalRoomId').value;
    const scheduleId = document.getElementById('modalScheduleId').value;

    // alert(day+ " docid: " + doctorId + " slotid: " + slotId + " roomid: " +roomId + " scheduleid: "+scheduleId);
    // return; //debug

    if (!scheduleId) {
        alert("Không xác định được lịch cần thay đổi.");
        return;
    }

    if (!confirm("Bạn có chắc chắn muốn lưu slot này?")) return;

    // Gửi request đến controller
    $.ajax({
        url: "/Schedule/UpdateSchedule", // đường dẫn controller
        type: "POST",
        data: { scheduleId: scheduleId, day:day, slotId: slotId, doctorId: doctorId, roomId: roomId },
        success: function (res) {
            if (res.success) {
                $('#editScheduleModal').modal('hide');
                alert("Đã thay đổi thành công.");
                showDoctorSchedule(); // cập nhật lại lịch
            } else {
                alert("Thay đổi thất bại: " + res.message);
            }
        },
        error: function () {
            alert("Có lỗi xảy ra khi thay đổi.");
        }
    });
}
