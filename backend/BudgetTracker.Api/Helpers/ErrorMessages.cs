namespace BudgetTracker.Api.Helpers;

/// <summary>
/// 영문 에러 코드를 한국어 사용자 메시지로 변환하는 공통 헬퍼
/// </summary>
public static class ErrorMessages
{
    public static string GetMessage(string code) => code switch
    {
        "NOT_FOUND"                => "리소스를 찾을 수 없습니다.",
        "CATEGORY_NOT_FOUND"       => "존재하지 않는 카테고리입니다.",
        "PAYMENT_METHOD_NOT_FOUND" => "존재하지 않는 결제수단입니다.",
        "POINT_INSUFFICIENT"       => "포인트 잔액이 부족합니다.",
        "MISSING_SEQ"              => "회차 번호가 필요합니다.",
        "MISSING_DATE"             => "date 파라미터가 필요합니다.",
        "MISSING_YEAR_MONTH"       => "year, month 파라미터가 필요합니다.",
        "INVALID_MODE"             => "유효하지 않은 모드입니다.",
        _                          => code,
    };
}
