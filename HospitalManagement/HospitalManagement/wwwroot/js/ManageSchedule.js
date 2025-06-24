

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
function collectSelectedSlots() {
    const selectedSlots = [];

    document.querySelectorAll(".slot-inactive.selected").forEach(div => {
        const date = div.closest(".schedule-slot").dataset.date;
        const slot = div.closest(".schedule-slot").dataset.slot;
        selectedSlots.push(`${date}_${slot}`);
    });

    return selectedSlots;
}
// click 1 slot
function toggleSlot(cardDiv) {
    const slotWrapper = cardDiv.closest(".schedule-slot");
    const slot = slotWrapper.dataset.slot;
    const date = slotWrapper.dataset.date;
    const slotId = `${date}_${slot}`;

    const index = selectedSlots.indexOf(slotId);
    const contentDivs = cardDiv.querySelectorAll("div");
    const contentDiv = contentDivs[1];

    if (index === -1) {
        selectedSlots.push(slotId);
        cardDiv.classList.remove("slot-inactive");
        cardDiv.classList.add("slot-active");
        contentDiv.innerHTML = '<i class="bi bi-check-circle-fill text-warning me-1"></i> Đã chọn';
    } else {
        selectedSlots.splice(index, 1);
        cardDiv.classList.remove("slot-active");
        cardDiv.classList.add("slot-inactive");
        contentDiv.innerHTML = '<i class="fas fa-bars"></i> Chưa chọn';
    }
}
function collectSelectedSlots() {
    return selectedSlots;
}

function submitAddDoctorToSlots() {
    document.getElementById("confirmBox").style.display = "none";

    const year = document.getElementById("yearDropdown").value;
    const weekStart = document.getElementById("weekDropdown").value;
    const doctorId = document.getElementById("doctorSelect").value;
    const roomId = document.getElementById("roomSelect").value;
    const departmentName = document.getElementById("depSelect").value;

    const slotStates = {};
    selectedSlots.forEach(slotId => {
        slotStates[`SlotStates[${slotId}]`] = "true";
    });

    const formData = new URLSearchParams();
    for (const key in slotStates) {
        formData.append(key, slotStates[key]);
    }
    formData.append("doctorId", doctorId);
    formData.append("roomId", roomId);
    formData.append("departmentName", departmentName);
    formData.append("year", year);
    formData.append("weekStart", weekStart);

    fetch("/Schedule/AddDoctorIntoSlot", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: formData.toString()
    })
    .then(res => res.text())
    .then(html => {
        document.getElementById("scheduleTable").innerHTML = html;
        selectedSlots = []; // reset
    })
    .catch(err => {
        alert("Lỗi khi gửi lịch.");
        console.error(err);
    });
}
