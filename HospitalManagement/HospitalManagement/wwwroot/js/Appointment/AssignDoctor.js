function prevDoctor() {
    const container = document.querySelector('.doctor-scroll-container');
    if (container) {
        container.scrollBy({ left: -200, behavior: 'smooth' });
        setTimeout(updateDoctorScrollVisibility, 300);
        console.log('Scrolled left');
    } else {
        console.error('Scroll container not found');
    }
}

function nextDoctor() {
    const container = document.querySelector('.doctor-scroll-container');
    if (container) {
        container.scrollBy({ left: 200, behavior: 'smooth' });
        setTimeout(updateDoctorScrollVisibility, 300);
        console.log('Scrolled right');
    } else {
        console.error('Scroll container not found');
    }
}
function updateDoctorScrollVisibility() {
    const container = document.querySelector('.doctor-scroll-container');
    if (!container) return;

    const prevButton = document.querySelector('.carousel-nav.prev');
    const nextButton = document.querySelector('.carousel-nav.next');

    // Log scroll info for debugging
    console.log('Scroll width:', container.scrollWidth, 'Client width:', container.clientWidth, 'Scroll left:', container.scrollLeft);

    // Show/hide buttons based on scroll position
    prevButton.style.display = container.scrollLeft <= 0 ? 'none' : 'block';
    nextButton.style.display = container.scrollLeft + container.clientWidth >= container.scrollWidth - 1 ? 'none' : 'block';
}


$(document).on("click", ".open-assign-modal", function (e) {
    e.preventDefault();
    const appId = $(this).data("appointment-id");
    const date = $(this).data("appointment-date");

    $.get("/Appointment/LoadAssignDoctorModal", { appointmentId: appId, date: date }, function (html) {
        $("#assignDoctorModalBody").html(html);
        const modal = new bootstrap.Modal(document.getElementById('assignDoctorModal'));
        modal.show();
    });
});

$(document).on('click', '.doctor-card', function () {
    $('.doctor-card').removeClass('selected');
    $(this).addClass('selected');
    const selectedId = $(this).data('doctor-id');
    $('#SelectedDoctorId').val(selectedId);
    console.log("Doctor ID: " + selectedId);
});

$("#doctorSearchInput").on("keyup", function () {
    const keyword = $(this).val().toLowerCase();
    $(".doctor-card").each(function () {
        const name = $(this).find(".card-title").text().toLowerCase();
        const dept = $(this).find(".text-muted").text().toLowerCase();
        $(this).toggle(name.includes(keyword) || dept.includes(keyword));
    });
});
