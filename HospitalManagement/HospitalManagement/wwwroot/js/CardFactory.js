
let selectedRole = null;
let selectedStaff = null;
let selectedRoom = null;
let selectedSlots = [];

const roleSelect = document.getElementById("roleSelect");
const staffSelect = document.getElementById("staffSelect");
const roomSelect = document.getElementById("roomSelect");
const card = document.getElementById("card");
const cardContent = document.getElementById("cardContent");

// Role change → update staff options
roleSelect.addEventListener("change", () => {
  selectedRole = roleSelect.value || null;

  // Reset nhân viên
  selectedStaff = null;
  staffSelect.value = "";
  staffSelect.innerHTML = '<option value="">Chọn nhân viên</option>';

  if (selectedRole) {
    if (selectedRole === "Doctor") {
      staffSelect.innerHTML += '<option value="Dr. A">Dr. A</option><option value="Dr. B">Dr. B</option>';
    } else if (selectedRole === "Cashier"){
      staffSelect.innerHTML += '<option value="Cashier A">Cashier A</option><option value="Cashier B">Cashier B</option>';
    } else{
      staffSelect.innerHTML += '<option value="Sale A">Sale A</option><option value="Sale B">Sale B</option>';
    }
    staffSelect.disabled = false;
  } else {
    staffSelect.disabled = true;
  }
  resetAllSlots(); // Reset bảng
  updateCard();
});


staffSelect.addEventListener("change", () => {
  selectedStaff = staffSelect.value || null;
  resetAllSlots(); // Reset bảng
  updateCard();
});

roomSelect.addEventListener("change", () => {
  selectedRoom = roomSelect.value || null;
  updateCard();
});

function updateCard() {
  cardContent.innerHTML = "";
  if (selectedRole) cardContent.innerHTML += `<div class="bi bi-person-badge me-1">      Role: ${selectedRole}</div>`;
  if (selectedStaff) cardContent.innerHTML += `<div class="bi bi-person-circle me-1">    Staff: ${selectedStaff}</div>`;
  if (selectedRoom) cardContent.innerHTML += `<div class="bi bi-building me-1">          Room: ${selectedRoom}</div>`;

  card.classList.remove("complete");

    // Nếu đủ cả 3 → thêm class 'complete' (đổi viền xanh)
    if (selectedRole && selectedStaff && selectedRoom) {
        card.classList.add("complete");
    }
}


// sự kiện trước khi add
document.getElementById("confirmAddBtn").addEventListener("click", () => {
    if (selectedSlots.length > 0 && selectedRole && selectedStaff && selectedRoom) {
        document.getElementById("confirmBox").style.display = "block";
    } else {
        if(selectedSlots.length == 0) alert("Vui lòng chọn ít nhất 1 slot.");
        else  alert("Vui lòng chọn đủ thông tin.");
    }
});

document.getElementById("btnNo").addEventListener("click", () => {
    document.getElementById("confirmBox").style.display = "none";
});

document.getElementById("btnYes").addEventListener("click", () => {
    document.getElementById("confirmBox").style.display = "none";
    
    selectedSlots.forEach(slotId => {
        const el = document.getElementById(slotId);
        const alreadyAssigned = el.dataset.assigned === "true"; // gán khi add trước đó

        if (alreadyAssigned) {
            el.classList.remove("slot-inactive", "slot-active","slot-success");
            el.classList.add("slot-fail");
            el.innerHTML = `
                <div><i class="fa fa-times text-danger me-1"></i> Đã có<br>${selectedStaff} trong slot</div>
            `;        
        } else {
            el.classList.remove("slot-inactive", "slot-active","slot-fail");
            el.classList.add("slot-success");
            el.innerHTML = `
                <div><i class="bi bi-check-circle-fill text-success me-1"></i> Đã thêm<br>${selectedStaff} vào slot</div>
            `;
            el.dataset.assigned = "true";

        }
    });

    selectedSlots = [];
});

// chọn slot
function toggleSlot(cardDiv) {
    // if (cardDiv.dataset.assigned === "true") return; // không cho chọn lại

    const slotWrapper = cardDiv.closest(".schedule-slot");
    const slot = slotWrapper.dataset.slot;
    const date = slotWrapper.dataset.date;
    const slotId = `${date}_${slot}`;

    const index = selectedSlots.indexOf(slotId);
    const hiddenInput = slotWrapper.querySelector("input[type='hidden']");
        // Lấy dòng nội dung thứ 2 của card
    const contentDivs = cardDiv.querySelectorAll("div");
    const contentDiv = contentDivs[1];
    
    if (index === -1) {
        selectedSlots.push(slotId);
        cardDiv.classList.remove("slot-inactive");
        cardDiv.classList.add("slot-active");
        hiddenInput.value = "true";
        contentDiv.innerHTML = '<i class="bi bi-check-circle-fill text-warning me-1"></i> Đã chọn';

    } else {
        selectedSlots.splice(index, 1);
        cardDiv.classList.remove("slot-active");
        cardDiv.classList.add("slot-inactive");
        hiddenInput.value = "false";
        contentDiv.innerHTML = '<i class="fas fa-bars"></i> Chưa chọn';

    }
}

// đổi người
function resetAllSlots() {
    selectedSlots = [];

    document.querySelectorAll(".schedule-slot").forEach(slot => {
        const card = slot.querySelector(".schedule-card");
        const hiddenInput = slot.querySelector("input[type='hidden']");

        const start = card.dataset.start;
        const end = card.dataset.end;

        // Xoá hết các class trạng thái trước
        card.classList.remove("slot-active", "slot-success", "slot-fail", "slot-inactive");

        // Thêm lại class mặc định
        card.classList.add("slot-inactive");


        card.innerHTML = `
            <div><i class="bi bi-clock me-1"></i> ${start} - ${end}</div>
            <div><i class="fas fa-bars"></i> Chưa chọn</div>
        `;

        card.dataset.assigned = "false";
        hiddenInput.value = "false";
    });
}
