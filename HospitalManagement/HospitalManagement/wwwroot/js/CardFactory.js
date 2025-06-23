let selectedDep = null;
let selectedDoctor = null;
let selectedRoom = null;
let selectedSlots = [];

const depSelect = document.getElementById("depSelect");
const doctorSelect = document.getElementById("doctorSelect");
const roomSelect = document.getElementById("roomSelect");
const card = document.getElementById("card");
const cardContent = document.getElementById("cardContent");


// Department
depSelect.addEventListener("change", () => {
  selectedDep = depSelect.value || null;

  // Reset doctor
doctorSelect.value = "";
doctorSelect.innerHTML = '<option value=""  >Mã bác sĩ</option>';
selectedDoctor = null; // confirm old doctor name is null in card

if (selectedDep) {

    const filteredDoctors = doctorList.filter(doc => doc.departmentName == selectedDep);
    // console.log("filteredDoctors:", filteredDoctors);
    // console.log("selectedDep:", selectedDep);
    // doctorList.forEach(doc => {
    //     console.log("Dep:", doc.departmentName);
    //     console.log("Name:", doc.fullName);

    // });
    // Thêm các option vào doctorSelect
    filteredDoctors.forEach(doc => {
      const option = document.createElement("option");
      option.value = doc.doctorId;
      option.textContent = doc.doctorCode;
    // console.log("Id:", doc.doctorId);

    //   console.log("Name:", doc.doctorCode);

      doctorSelect.appendChild(option);
    });

    doctorSelect.disabled = false;
  }
  else {
    doctorSelect.disabled = true;
  }

  updateSchedule(); // Reset bảng
  updateCard();
});

// doctor 
doctorSelect.addEventListener("change", () => {
  selectedDoctor = doctorSelect.value 
    ? doctorSelect.options[doctorSelect.selectedIndex].text 
    : null;
  updateSchedule(); // Reset bảng
  updateCard();
});

// room
roomSelect.addEventListener("change", () => {
  selectedRoom = roomSelect.value 
    ? roomSelect.options[roomSelect.selectedIndex].text 
    : null;
  updateCard();
});

// update card
function updateCard() {
  cardContent.innerHTML = ""; 
  if (selectedDep) cardContent.innerHTML += `<div> <div> <i class="fa fa-plus-circle" aria-hidden="true"></i> Khoa: ${selectedDep}</div>`;
  if (selectedDoctor) cardContent.innerHTML += `<div> <i class="fa fa-user-md" aria-hidden="true"></i>   Bác sĩ: ${selectedDoctor}</div>`;
  if (selectedRoom) cardContent.innerHTML += `<div> <i class="fa fa-bed" aria-hidden="true"></i> Phòng: ${selectedRoom}</div>`;

  card.classList.remove("complete");

    // Nếu đủ cả 3 → thêm class 'complete' (đổi viền xanh)
    if (selectedDep && selectedDoctor && selectedRoom) {
        card.classList.add("complete");
    }
}


// hỏi lại trước khi add
document.getElementById("confirmAddBtn").addEventListener("click", () => {
    if (selectedSlots.length > 0 && selectedDep && selectedDoctor && selectedRoom) {
        document.getElementById("confirmBox").style.display = "block";
    } else {
        if(selectedSlots.length == 0) alert("Vui lòng chọn ít nhất 1 slot.");
        else  alert("Vui lòng chọn đủ thông tin.");
    }
});

// no
document.getElementById("btnNo").addEventListener("click", () => {
    document.getElementById("confirmBox").style.display = "none";
});

