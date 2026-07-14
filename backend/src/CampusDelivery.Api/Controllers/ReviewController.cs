using CampusDelivery.Api.Models;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CampusDelivery.Api.Controllers;

[Authorize]  
public class ReviewController : Controller
{
    private readonly ReviewService _reviewService;

    public ReviewController(ReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    //普通用户操作

    // 查看某个报告的所有评价
    [HttpGet]
    public IActionResult Index(int reportId)
    {
        var reviews = _reviewService.GetReviewsByReportId(reportId);
        return View(reviews);
    }

    //显示添加评价页面
    [HttpGet]
    public IActionResult Create(int reportId)
    {
        var model = new Review
        {
            ReportID = reportId,
            Reviewed_at = DateTime.Now
        };
        return View(model);
    }

    // 提交添加评价
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Review review)
    {
        if (!ModelState.IsValid)
        {
            return View(review);
        }

        var (success, error) = _reviewService.AddReview(review);
        if (success)
        {
            TempData["SuccessMessage"] = "评价提交成功";
            return RedirectToAction(nameof(Index), new { reportId = review.ReportID });
        }

        ModelState.AddModelError(string.Empty, error);
        return View(review);
    }

    //管理员专用操作

    // 管理员查看所有评价（分页）
    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public IActionResult All(int page = 1, int size = 20)
    {
        var reviews = _reviewService.GetAllReviews(page, size);
        var total = _reviewService.GetTotalCount();

        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.Size = size;
        return View(reviews);
    }

    // 管理员删除评价
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var (success, error) = _reviewService.DeleteReview(id);
        if (success)
        {
            TempData["SuccessMessage"] = "评价已删除";
        }
        else
        {
            TempData["ErrorMessage"] = error;
        }
        return RedirectToAction(nameof(All));
    }

    // 管理员编辑评价页面
    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var review = _reviewService.GetReviewById(id);
        if (review == null)
        {
            return NotFound();
        }
        return View(review);
    }

    // 提交编辑评价

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Review review)
    {
        if (!ModelState.IsValid)
        {
            return View(review);
        }

        var (success, error) = _reviewService.UpdateReview(review);
        if (success)
        {
            TempData["SuccessMessage"] = "评价已更新";
            return RedirectToAction(nameof(All));
        }

        ModelState.AddModelError(string.Empty, error);
        return View(review);
    }
}
