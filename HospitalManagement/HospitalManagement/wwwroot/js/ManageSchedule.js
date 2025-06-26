// ========== Biến toàn cục ==========
let selectedDep = null;
let selectedDoctor = null;
let selectedRoom = null;
let selectedSlots = [];

// ========== DOM ==========
const depSelect = document.getElementById("depSelect");
const doctorSelect = document.getElementById("doctorSelect");
const roomSelect = document.getElementById("roomSelect");
const card = document.getElementById("card");
const cardContent = document.getElementById("cardContent");

// ========== Sự kiện chọn khoa ==========
function changeDepartment(){
    selectedDep = depSelect.value || null;

    // Reset bác sĩ
    doctorSelect.value = "";
    doctorSelect.innerHTML = '<option value="">Mã bác sĩ</option>';
    selectedDoctor = null;

    if (selectedDep) {
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

// ========== Sự kiện chọn phòng ==========
function changeRoom(){
    selectedRoom = roomSelect.value
        ? roomSelect.options[roomSelect.selectedIndex].text
        : null;
    updateCard();
}

// ========== Cập nhật thẻ card ==========
function updateCard() {
    cardContent.innerHTML = "";

    if (selectedDep) cardContent.innerHTML += `<div><i class="fa fa-plus-circle"></i> Khoa: ${selectedDep}</div>`;
    if (selectedDoctor) cardContent.innerHTML += `<div><i class="fa fa-user-md"></i> Bác sĩ: ${selectedDoctor}</div>`;
    if (selectedRoom) cardContent.innerHTML += `<div><i class="fa fa-bed"></i> Phòng: ${selectedRoom}</div>`;

    card.classList.remove("complete");
    if (selectedDep && selectedDoctor && selectedRoom) {
        card.classList.add("complete");
    }
}

// ========== Toggle slot ==========
function toggleSlot(cardDiv) {
    const slotWrapper = cardDiv.closest(".schedule-slot");
    const slot = slotWrapper.dataset.slot;
    const date = slotWrapper.dataset.date;
    const slotId = `${date}_${slot}`;
    const contentDiv = cardDiv.querySelectorAll("div")[1];
    const hiddenInput = cardDiv.parentElement.querySelector("input[type=hidden]");

    const index = selectedSlots.indexOf(slotId);
    if (index === -1) {
        selectedSlots.push(slotId);
        cardDiv.classList.remove("slot-inactive");
        cardDiv.classList.add("slot-active");
        contentDiv.innerHTML = '<i class="bi bi-check-circle-fill text-warning me-1"></i> Đã chọn';
        if (hiddenInput) hiddenInput.value = "true";
    } else {
        selectedSlots.splice(index, 1);
        cardDiv.classList.remove("slot-active");
        cardDiv.classList.add("slot-inactive");
        contentDiv.innerHTML = '<i class="fas fa-bars"></i> Chưa chọn';
        if (hiddenInput) hiddenInput.value = "false";
    }
}

// ========== Reset toàn bộ slot ==========
function resetSlotStates() {
    document.querySelectorAll('.schedule-card.slot-active').forEach(el => {
        el.classList.remove('slot-active');
        el.classList.add('slot-inactive');
        el.setAttribute('data-assigned', 'false');

        el.querySelector('div:last-child').innerHTML = '<i class="fas fa-bars"></i> Chưa chọn';

        const slotKey = el.id;
        const input = document.querySelector(`input[name="SlotStates[${slotKey}]"]`);
        if (input) input.value = "false";
    });

    selectedSlots = [];
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
        ? (new Date(newyear, 0, 1)).toISOString().split('T')[0]
        : document.getElementById("weekDropdown").value;

    $.ajax({
        url: '/Schedule/GetScheduleTable2',
        type: 'GET',
        data: { year, weekStart },
        success: function (html) {
            $('#scheduleTable').html(html);
            resetSlotStates();
        },
        error: function () {
            alert("Lỗi khi tải lịch làm việc.");
        }
    });
}

// ========== Hiện hộp xác nhận ==========
function showConfirmBox(){
    if (selectedSlots.length > 0 && selectedDep && selectedDoctor && selectedRoom) {
        document.getElementById("confirmBox").style.display = "block";
    } else {
        if (selectedSlots.length === 0) alert("Vui lòng chọn ít nhất 1 slot.");
        else alert("Vui lòng chọn đủ thông tin.");
    }
}
// ========== Ẩn hộp xác nhận ==========

function hideConfirmBox(){
    document.getElementById("confirmBox").style.display = "none";

}
// ========== Gửi lịch làm việc mới của bác sĩ ==========
function submitAddDoctorToSlots() {
    hideConfirmBox(); // Ẩn hộp xác nhận

    const year = document.getElementById("yearDropdown").value;
    const weekStart = document.getElementById("weekDropdown").value;
    const doctorId = document.getElementById("doctorSelect").value;
    const roomId = document.getElementById("roomSelect").value;
    const departmentName = document.getElementById("depSelect").value;

    const formData = new FormData();

    // Thêm các slot đã chọn
    selectedSlots.forEach(slotId => {
        formData.append(`SlotStates[${slotId}]`, "true");
    });

    // Thêm thông tin bác sĩ, phòng, khoa, tuần
    formData.append("doctorId", doctorId);
    formData.append("roomId", roomId);
    formData.append("departmentName", departmentName);
    formData.append("year", year);
    formData.append("weekStart", weekStart);

    console.log(failList.length);
    console.log(successList.length);
    console.log(workingList.length);


    // Thêm các list đã tồn tại trước đó
    failList.forEach((item, index) => {
            formData.append(`existingFailList[${index}]`, item);
    });
    successList.forEach((item, index) => {
        formData.append(`existingSuccessList[${index}]`, item);
    });
    workingList.forEach((item, index) => {
        formData.append(`existingWorkingList[${index}]`, item);
    });


    // Gửi AJAX
    $.ajax({
        url: "/Schedule/AddDoctorIntoSlot",
        method: "POST",
        data: formData,
        contentType: false,
        processData: false,
        success: function (html) {
            $("#scheduleTable").html(html);
            selectedSlots = []; // reset sau khi gửi thành công
        },
        error: function () {
            alert("Lỗi khi gửi lịch làm việc.");
        }
    });
}


// ========== reset selected slot ==========

function resetSelectedSlots() {
    document.querySelectorAll('.schedule-card.slot-active').forEach(el => {
        el.classList.remove("slot-active");
        el.classList.add("slot-inactive");
        el.querySelector("div:last-child").innerHTML = '<i class="fas fa-bars"></i> Chưa chọn';
        const input = el.parentElement.querySelector("input[type=hidden]");
        if (input) input.value = "false";
    });
    selectedSlots = [];
}
// ========== show working schedule ==========
function showDoctorSchedule() {
    if (!selectedDoctor) {
        alert("Vui lòng chọn bác sĩ.");
        return;
    }

    const button = document.getElementById("showScheduleBtn");

    if (!button || button.disabled) return;

    button.disabled = true;

    // Delay 2 giây rồi mới gửi
    setTimeout(() => {
        $.ajax({
            url: '/Schedule/GetDoctorScheduleInWeek',
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
    }, 1100); // delay 1.1 giây

}
